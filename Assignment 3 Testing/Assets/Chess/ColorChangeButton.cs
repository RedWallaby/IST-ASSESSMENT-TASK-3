using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorChangeButton : MonoBehaviour
{
    public void SwitchColor()
    {
        Image image = GetComponent<Image>();
        image.color = image.color == new Color(0.75f, 0.75f, 0.75f) ? Color.white : new Color(0.75f, 0.75f, 0.75f);
        ReplacementOption option = GetComponentInParent<ReplacementOption>();
        option.ChangePieceColor();
        if (option == option.cursor.currentOption)
        {
            option.cursor.GetComponent<Image>().color = image.color;
        }
    }
}
