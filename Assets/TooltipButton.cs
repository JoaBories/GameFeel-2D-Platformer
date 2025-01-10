using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TooltipButton : MonoBehaviour
{
    [SerializeField] private GameObject tooltip;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ShowTooltip);
    }

    private void ShowTooltip()
    {
        if (tooltip.activeSelf)
        {
            tooltip.SetActive(false);
        }
        else
        {
            tooltip.SetActive(true);
        }
    }
}
