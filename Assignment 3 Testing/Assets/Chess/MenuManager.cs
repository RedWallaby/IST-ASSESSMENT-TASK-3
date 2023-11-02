using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using TMPro;
using UnityEngine;
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

        cursor.ResetCursor();
        if (index == -2)
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
            //nothing
        }
        if (index == 0) //main menu
        {
            returnToMenu.SetActive(false);
            //nothing
        }
        if (index == 1) //piece adding
        {
            returnToMenu.SetActive(true);
            isTutorial = false;
            isRunning = false;
            ResetBoard();
            //addingMenu.SetActive(!addingMenu.activeInHierarchy);
        }
        else
        {
            //ChessBoard.isRunning = false; //maybe
        }

        foreach (Transform transform in transform)
        {
            transform.gameObject.SetActive(transform == obj);
            /*if (transform == obj)
            {
                transform.gameObject.SetActive(true);
            }
            else
            {
                transform.gameObject.SetActive(false);
            }*/
        }
    }

    public void SetGameOver(string str)
    {
        Transform trnsform = transform.GetChild(3);
        trnsform.GetChild(0).GetComponent<TMP_Text>().text = str + " Wins!";
    }

    public void SetPlaceStyle(TMP_Dropdown dropdown)
    {
        isReplacements = dropdown.value == 0;
        cursor.ResetCursor();
        cursor.HideCursor();
        ResetBoard();
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
