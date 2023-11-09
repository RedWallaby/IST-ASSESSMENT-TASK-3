using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ChessBoard;

public class TutorialManager : MonoBehaviour
{
    public int stepProgress;
    public int autoProgress;
    private bool isStepNext;
    public bool readyForNextMove;



    //setup of tutorial moves
    public List<ManualSlots> allOrders;
    public List<AutoMoves> allAutoMoves;

    private List<Slot> currentOrder;
    private List<Move> currentAutoMoves;

    [System.Serializable]
    public class ManualSlots
    {
        public List<Slot> orderSlots;
    }

    [System.Serializable]
    public class AutoMoves
    {
        public List<Move> orderSlots;
    }

    [System.Serializable]
    public struct Move
    {
        public Slot startSlot;
        public Slot endSlot;
    }



    //setup of boards
    public List<ListWrapper> allTutorialSlots;

    [System.Serializable]
    public struct ChessSlot
    {
        public int index;
        public Piece piece;
        public bool isWhite;
    }

    [System.Serializable]
    public class ListWrapper
    {
        public List<ChessSlot> tutorialSlots;
    }



    //tutorial text
    public TMP_Text text;
    public List<Texts> allTexts;

    [System.Serializable]
    public class Texts
    {
        public List<string> texts;
    }

    public void SetupTutorial() //reset the tutorial values
    {
        currentOrder = allOrders[currentTutorial].orderSlots;
        currentAutoMoves = allAutoMoves[currentTutorial].orderSlots;

        readyForNextMove = false;
        isStepNext = false;
        autoProgress = 0;
        stepProgress = 0;
        NextStep();
    }

    public void NextStep() //create the next action in the tutorial
    {
        if (isStepNext) //determine if the next move should be an ai move or made by the player
        {
            SelectSlot(currentOrder[stepProgress]);
            if (stepProgress % 2 == 1) isStepNext = false;
            stepProgress++;
        }
        else
        {
            text.transform.parent.gameObject.SetActive(false);
            readyForNextMove = false;
            Invoke(nameof(AiMove), 0.3f);
        }
    }

    public void AiMove()
    {
        if (autoProgress >= currentAutoMoves.Count)
        {
            print("next!");
            NextTutorial();
            return;
        }
        isWhiteTurn = !isWhiteTurn;
        currentAutoMoves[autoProgress].startSlot.MovePieceToSlot(currentAutoMoves[autoProgress].endSlot);
        text.transform.parent.gameObject.SetActive(true);
        text.text = allTexts[currentTutorial].texts[autoProgress];
        CancelInvoke(nameof(ReSetText));
        readyForNextMove = true;
        isStepNext = true;
        autoProgress++;
        NextStep();
    }

    public void NextTutorial() //proceed to the next tutorial
    {
        if (currentTutorial < 4)
        {
            SetBoardToTutorial(currentTutorial + 1);
        }
        else
        {
            Invoke(nameof(SendBack), 1);
        }
    }

    public void SendBack() //return to menu
    {
        gameObject.SetActive(false);
        menus.SetMenu(0);
        menus.HideChessBoard();
    }

    public void SelectSlot(Slot newSlot) //makes this slot the only one clickable
    {
        if (currentTutorialSlot != null) currentTutorialSlot.ResetBaseColour();
        currentTutorialSlot = newSlot;
        newSlot.GetComponent<Image>().color = Color.magenta;
    }

    public void ReSetText()
    {
        text.text = allTexts[currentTutorial].texts[autoProgress-1];
    }
}
