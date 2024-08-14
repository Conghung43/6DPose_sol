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
    [SerializeField] private TextAndVideo _home;
    [SerializeField] private TextAndVideo _sample;

    private Dictionary<string, TextAndVideo> _desDictionary;

    private void Start()
    {
        _desDictionary=new Dictionary<string, TextAndVideo>
        {
            {"Home",_home},{"Sample",_sample}
        };
    }

    public void UpdateDescription(string functionName )
    {
        if (!_desDictionary.ContainsKey(functionName))
        {
            _descriptionPanel.SetActive(false);
        }
        else
        {
            _descriptionPanel.SetActive(true);
            _desText.text = _desDictionary[functionName].des;
            _videoPlayer.clip = _desDictionary[functionName].clip;
            _videoPlayer.Play();
        }
        
    }
}
