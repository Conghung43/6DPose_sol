using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Video;

[Serializable]
public class TextAndVideo
{
    public string des;
    public VideoClip clip;
}
public class DescriptionController : MonoBehaviour
{
    [SerializeField] private GameObject _descriptionPanel;
    [SerializeField] private TMP_Text _desText;
    [SerializeField] private VideoPlayer _videoPlayer;
    [FormerlySerializedAs("_home")] [SerializeField] private TextAndVideo _stage1;
    [FormerlySerializedAs("_sample")] [SerializeField] private TextAndVideo _stage2;
    [FormerlySerializedAs("_detect")] [SerializeField] private TextAndVideo _stage3;
    [FormerlySerializedAs("_result")] [SerializeField] private TextAndVideo _stage4;

    private Dictionary<int, TextAndVideo> _desDictionary;

    void OnEnable()
    {
        _desDictionary = new Dictionary<int, TextAndVideo>
        {
            { 1, _stage1 }, { 2, _stage2 }, { 3, _stage3 },{ 4, _stage4 }

        };
    }

    public void UpdateDescription(string functionName,int stageIndex )
    {
        if (functionName!="Sample"||!_desDictionary.ContainsKey(stageIndex))
        {
            _descriptionPanel.SetActive(false);
        }
        else
        {
            _descriptionPanel.SetActive(true);
            _desText.text = _desDictionary[stageIndex].des;
            _videoPlayer.clip = _desDictionary[stageIndex].clip;
            _videoPlayer.Play();
        }
        
    }
}
