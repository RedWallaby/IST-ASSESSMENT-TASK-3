using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ChessBoard;

public class MenuManager : MonoBehaviour
{
    public Cursor cursor;

    public GameObject addingMenu;

    public GameObject chessBoard;

    public GameObject returnToMenu;

    public void SetMenu(int index)
    { 
        Transform obj = null;
        if (index > -1) obj = transform.GetChild(index);

        foreach (Transform transform in transform)
        {
            transform.gameObject.SetActive(transform == obj);
        }

        cursor.ResetCursor();
        if (index == -2) //tutorial
        {
            isRunning = true;
            isWhiteTurn = false;
            cursor.HideCursor();
            SetBoardToTutorial(0);
            isTutorial = true;
            tutorialManager.gameObject.SetActive(true);
        }
        if (index == -1) //hide all menus for game
        {
            isRunning = true;
            isWhiteTurn = true;
            cursor.HideCursor();
        }
        if (index == 0) //main menu
        {
            returnToMenu.SetActive(false);
        }
        if (index == 1) //piece adding
        {
            returnToMenu.SetActive(true);
            isTutorial = false;
            isRunning = false;
            ResetBoard(true);
        }
    }

    public void SetGameOver(string str)
    {
        Transform trnsform = transform.GetChild(3);
        trnsform.GetChild(1).GetComponent<TMP_Text>().text = str + " Wins!";
        trnsform.GetChild(0).GetComponent<Image>().sprite = extraSprites[str == "White" ? 1 : 2];
    }

    public void SetPlaceStyle(TMP_Dropdown dropdown) //change from replacements to free place
    {
        isReplacements = dropdown.value == 0;
        cursor.ResetCursor();
        cursor.HideCursor();
        if (isReplacements)
        {
            ResetBoard(true);
        }
        else
        {
            WipeBoard();
        }
    }

        public void ShowChessBoard()
    {
        board.gameObject.SetActive(true);
    }

    public void HideChessBoard()
    {
        board.gameObject.SetActive(false);
    }
}
