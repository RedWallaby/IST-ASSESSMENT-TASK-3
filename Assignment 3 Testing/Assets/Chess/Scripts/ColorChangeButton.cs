using UnityEngine;
using UnityEngine.UI;

public class ColorChangeButton : MonoBehaviour
{
    public void SwitchColor(bool bypass) //changes the colour of the piece you will place
    {
        if (ChessBoard.isReplacements && !bypass) return;
        Image image = GetComponent<Image>();
        image.color = image.color == Color.black ? Color.white : Color.black;
        ReplacementOption option = GetComponentInParent<ReplacementOption>();
        option.ChangePieceColor();
        if (option == option.cursor.currentOption)
        {
            option.cursor.GetComponent<Image>().sprite = option.piece.isWhite ? option.piece.sprite : option.piece.sprite2;
        }
    }
}
