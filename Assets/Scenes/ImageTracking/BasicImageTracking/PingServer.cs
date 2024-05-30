using UnityEngine;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEngine.UI;
using TMPro;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class PingServer : MonoBehaviour
    {
        public float pingInterval = 5.0f;   // Time interval between pings in seconds
        public TMP_InputField inputField;
        void Start()
        {
            // Start the pinging process
            //StartPing();
            inputField.onEndEdit.AddListener(OnEndEdit);
            Inference.ip = LoadKey();
            inputField.text = Inference.ip;
        }

        void OnEndEdit(string inputText)
        {
            Inference.ip = inputText;
            SaveKey(inputText);
        }

        private const string KeyName = "ip";

        // Save the key to PlayerPrefs
        public void SaveKey(string key)
        {
            PlayerPrefs.SetString(KeyName, key);
            PlayerPrefs.Save();
            Debug.Log("Key saved: " + key);
        }

        // Load the key from PlayerPrefs
        public string LoadKey()
        {
            if (PlayerPrefs.HasKey(KeyName))
            {
                string key = PlayerPrefs.GetString(KeyName);
                Debug.Log("Key loaded: " + key);
                return key;
            }
            else
            {
                Debug.LogWarning("No key found.");
                return "";
            }
        }

        //async void StartPing()
        //{
        //    while (true)
        //    {
        //        bool isAlive = await PingAsync(Inference.ip);
        //        Debug.Log("Server " + Inference.ip + " is " + (isAlive ? "alive" : "not alive"));
        //        await Task.Delay((int)(pingInterval * 1000)); // Wait for the specified interval before pinging again
        //    }
        //}

        //Task<bool> PingAsync(string ip)
        //{
        //    return Task.Run(() =>
        //    {
        //        try
        //        {
        //            System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
        //            PingReply reply = ping.Send(ip, 1000); // Timeout set to 1000 milliseconds (1 second)
        //        return reply.Status == IPStatus.Success;
        //        }
        //        catch
        //        {
        //            return false;
        //        }
        //    });
        //}
    }

}