import ARKit
import AVKit
import SwiftUI
import RealityKit

import Foundation
import CoreGraphics
import MetalKit
import Accelerate

let arKitSession = ARKitSession()
var isRunning: Bool = false
var texturePointer: UnsafeMutableRawPointer?
var intrinsicsPointer: UnsafeMutableRawPointer?
var extrinsicsPointer: UnsafeMutableRawPointer?
var currentTexture: MTLTexture?
let mtlDevice: MTLDevice = MTLCreateSystemDefaultDevice()!
var textureCache: CVMetalTextureCache! = nil
var commandQueue: MTLCommandQueue!

@_cdecl("askCameraPermission")
func askCameraPermission() {
    print("############ Ask Permission ##############")
    Task {
        CameraVideoFormat.supportedVideoFormats(for: .main, cameraPositions: [.left])
        await arKitSession.queryAuthorization(for: [.cameraAccess])
    }
}

@_cdecl("startVisionProMainCamera")
func startVisionProMainCamera() {
    print("############ START ##############")

    guard !isRunning else {
            print("Camera capture is already running.")
            return
        }

    isRunning = true

    Task {
        let cameraTracking = CameraFrameProvider()
        do { try await arKitSession.run([cameraTracking]) } catch { return }

        // Then receive the new camera frame:
        for await cameraFrameUpdate in cameraTracking.cameraFrameUpdates(
            for: .supportedVideoFormats(for: .main, cameraPositions: [.left]).first!)!
        {
            if !isRunning {break}

            setPointerPixelBuffer(cameraFrameUpdate.primarySample.pixelBuffer)
            let parameters = cameraFrameUpdate.primarySample.parameters
            storeCameraParameters(intrinsics: parameters.intrinsics, extrinsics: parameters.extrinsics)
        }
    }
}

@_cdecl("stopVisionProMainCamera")
func stopVisionProMainCamera() {
    print("############ STOP ##############")
    
    isRunning = false
    
    arKitSession.stop()
}

func setPointerPixelBuffer(_ pixelBuffer: CVPixelBuffer) {
    guard let bgraBuffer = try? pixelBuffer.toBGRA() else { return }
    CVPixelBufferLockBaseAddress(bgraBuffer, .readOnly)
        
    let baseAddress = CVPixelBufferGetBaseAddress(bgraBuffer)
    let width = CVPixelBufferGetWidth(bgraBuffer)
    let height = CVPixelBufferGetHeight(bgraBuffer)
    let bytesPerRow = CVPixelBufferGetBytesPerRow(bgraBuffer)
        
    let bufferSize = height * bytesPerRow
    if texturePointer == nil {
        texturePointer = UnsafeMutableRawPointer.allocate(byteCount: bufferSize, alignment: 1)
    }

    for row in 0..<height {
        let rowStart = baseAddress! + row * bytesPerRow
        let rowEnd = baseAddress! + (height - row - 1) * bytesPerRow

        memcpy(texturePointer! + row * bytesPerRow, rowEnd, bytesPerRow)
    }
        
    CVPixelBufferUnlockBaseAddress(bgraBuffer, .readOnly)
}

extension CVPixelBuffer {
    public func toBGRA() throws -> CVPixelBuffer? {
        let pixelBuffer = self

        /// Check format
        let pixelFormat = CVPixelBufferGetPixelFormatType(pixelBuffer)
        guard pixelFormat == kCVPixelFormatType_420YpCbCr8BiPlanarFullRange else { return pixelBuffer }

        /// Split plane
        let yImage: VImage = pixelBuffer.with({ VImage(pixelBuffer: $0, plane: 0) })!
        let cbcrImage: VImage = pixelBuffer.with({ VImage(pixelBuffer: $0, plane: 1) })!

        /// Create output pixelBuffer
        let outPixelBuffer = CVPixelBuffer.make(width: yImage.width, height: yImage.height, format: kCVPixelFormatType_32BGRA)!

        /// Convert yuv to argb
        var argbImage = outPixelBuffer.with({ VImage(pixelBuffer: $0) })!
        try argbImage.draw(yBuffer: yImage.buffer, cbcrBuffer: cbcrImage.buffer)
        /// Convert argb to bgra
        argbImage.permute(channelMap: [3, 2, 1, 0])

        return outPixelBuffer
    }
}

struct VImage {
    let width: Int
    let height: Int
    let bytesPerRow: Int
    var buffer: vImage_Buffer

    init?(pixelBuffer: CVPixelBuffer, plane: Int) {
        guard let rawBuffer = CVPixelBufferGetBaseAddressOfPlane(pixelBuffer, plane) else { return nil }
        self.width = CVPixelBufferGetWidthOfPlane(pixelBuffer, plane)
        self.height = CVPixelBufferGetHeightOfPlane(pixelBuffer, plane)
        self.bytesPerRow = CVPixelBufferGetBytesPerRowOfPlane(pixelBuffer, plane)
        self.buffer = vImage_Buffer(
            data: UnsafeMutableRawPointer(mutating: rawBuffer),
            height: vImagePixelCount(height),
            width: vImagePixelCount(width),
            rowBytes: bytesPerRow
        )
    }

    init?(pixelBuffer: CVPixelBuffer) {
        guard let rawBuffer = CVPixelBufferGetBaseAddress(pixelBuffer) else { return nil }
        self.width = CVPixelBufferGetWidth(pixelBuffer)
        self.height = CVPixelBufferGetHeight(pixelBuffer)
        self.bytesPerRow = CVPixelBufferGetBytesPerRow(pixelBuffer)
        self.buffer = vImage_Buffer(
            data: UnsafeMutableRawPointer(mutating: rawBuffer),
            height: vImagePixelCount(height),
            width: vImagePixelCount(width),
            rowBytes: bytesPerRow
        )
    }

    mutating func draw(yBuffer: vImage_Buffer, cbcrBuffer: vImage_Buffer) throws {
        try buffer.draw(yBuffer: yBuffer, cbcrBuffer: cbcrBuffer)
    }

    mutating func permute(channelMap: [UInt8]) {
        buffer.permute(channelMap: channelMap)
    }
}


extension CVPixelBuffer {
    func with<T>(_ closure: ((_ pixelBuffer: CVPixelBuffer) -> T)) -> T {
        CVPixelBufferLockBaseAddress(self, .readOnly)
        let result = closure(self)
        CVPixelBufferUnlockBaseAddress(self, .readOnly)
        return result
    }

    static func make(width: Int, height: Int, format: OSType) -> CVPixelBuffer? {
        var pixelBuffer: CVPixelBuffer? = nil
        CVPixelBufferCreate(kCFAllocatorDefault,
                            width,
                            height,
                            format,
                            [String(kCVPixelBufferIOSurfacePropertiesKey): [
                                "IOSurfaceOpenGLESFBOCompatibility": true,
                                "IOSurfaceOpenGLESTextureCompatibility": true,
                                "IOSurfaceCoreAnimationCompatibility": true,
                            ]] as CFDictionary,
                            &pixelBuffer)
        return pixelBuffer
    }
}

extension vImage_Buffer {
    mutating func draw(yBuffer: vImage_Buffer, cbcrBuffer: vImage_Buffer) throws {
        var yBuffer = yBuffer
        var cbcrBuffer = cbcrBuffer
        var conversionMatrix: vImage_YpCbCrToARGB = {
            var pixelRange = vImage_YpCbCrPixelRange(Yp_bias: 0, CbCr_bias: 128, YpRangeMax: 255, CbCrRangeMax: 255, YpMax: 255, YpMin: 1, CbCrMax: 255, CbCrMin: 0)
            var matrix = vImage_YpCbCrToARGB()
            vImageConvert_YpCbCrToARGB_GenerateConversion(kvImage_YpCbCrToARGBMatrix_ITU_R_709_2, &pixelRange, &matrix, kvImage420Yp8_CbCr8, kvImageARGB8888, UInt32(kvImageNoFlags))
            return matrix
        }()
        let error = vImageConvert_420Yp8_CbCr8ToARGB8888(&yBuffer, &cbcrBuffer, &self, &conversionMatrix, nil, 255, UInt32(kvImageNoFlags))
        if error != kvImageNoError {
            fatalError()
        }
    }

    mutating func permute(channelMap: [UInt8]) {
        vImagePermuteChannels_ARGB8888(&self, &self, channelMap, 0)
    }
}

func storeCameraParameters(intrinsics: simd_float3x3, extrinsics: simd_float4x4) {
    var intrinsicsArray: [Float] = [
            intrinsics.columns.0.x, intrinsics.columns.0.y, intrinsics.columns.0.z,
            intrinsics.columns.1.x, intrinsics.columns.1.y, intrinsics.columns.1.z,
            intrinsics.columns.2.x, intrinsics.columns.2.y, intrinsics.columns.2.z
        ]

        var extrinsicsArray: [Float] = []
        for i in 0..<4 {
            for j in 0..<4 {
                extrinsicsArray.append(extrinsics[j][i]) // column-major 展開
            }
        }

        if intrinsicsPointer == nil {
            intrinsicsPointer = UnsafeMutableRawPointer.allocate(
                byteCount: 9 * MemoryLayout<Float>.size,
                alignment: MemoryLayout<Float>.alignment
            )
        }

        if extrinsicsPointer == nil {
            extrinsicsPointer = UnsafeMutableRawPointer.allocate(
                byteCount: 16 * MemoryLayout<Float>.size,
                alignment: MemoryLayout<Float>.alignment
            )
        }

        intrinsicsPointer?.copyMemory(from: &intrinsicsArray, byteCount: 9 * MemoryLayout<Float>.size)
        extrinsicsPointer?.copyMemory(from: &extrinsicsArray, byteCount: 16 * MemoryLayout<Float>.size)

}

@_cdecl("getTexturePointer")
public func getTexturePointer() -> UnsafeMutableRawPointer? {
    return texturePointer
}

@_cdecl("getIntrinsicsPointer")
public func getIntrinsicsPointer() -> UnsafeMutableRawPointer? {
    return intrinsicsPointer
}

@_cdecl("getExtrinsicsPointer")
public func getExtrinsicsPointer() -> UnsafeMutableRawPointer? {
    return extrinsicsPointer
}
