using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CheckListController : MonoBehaviour
{
    [SerializeField] private List<Image> checkMarks;
    [SerializeField] private List<Image> resultMarks;
    [SerializeField] private List<GameObject> backGrounds;
    [SerializeField] private Sprite okTexture;
    [SerializeField] private Sprite ngTexture;
    [SerializeField] private Sprite spriteTexture;

    void Start()
    {
        EventManager.OnStageChange += OnChangeCheckListUI;
    }

    public void ResetResult()
    {
        foreach (var checkMark in checkMarks)
        {
            checkMark.sprite = spriteTexture;
        }
    }

    public void SetResult(bool success, int index)
    {
        if (index < 0 || checkMarks.Count < index) return;

        if (success)
        {
            checkMarks[index].sprite = okTexture;
            resultMarks[index].sprite = okTexture;
        }
        else
        {
            checkMarks[index].sprite = ngTexture;
            resultMarks[index].sprite = ngTexture;
        }
    }

    private void OnChangeCheckListUI(object sender, EventManager.OnStageIndexEventArgs e)
    {
        for (int i = 0; i < backGrounds.Count; i++)
        {
            if (i == StationStageIndex.stageIndex - 1)
            {
                backGrounds[i].SetActive(true);
            }
            else
            {
                backGrounds[i].SetActive(false);
            }
        }
    }
}