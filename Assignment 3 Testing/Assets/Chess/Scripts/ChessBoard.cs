using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

//[ExecuteInEditMode]
public class ChessBoard : MonoBehaviour
{
    public static ChessBoard current; //singleton (I can do this or make everything reference each other, this is probably cleaner)

    public static Transform board;
    public Transform chessBoard;

    public static MenuManager menus;
    public MenuManager menuManager;

    public static bool isReplacements = true;
    public static bool isRunning;
    public static bool isWhiteTurn = true;
    public static bool moveInProgress;

    public static Slot whiteKingSlot;
    public static Slot blackKingSlot;

    public static List<Piece> defaultBoard;
    public List<Piece> defaultBoardP;

    public static List<Sprite> extraSprites;
    public List<Sprite> extraSpritesP;

    //tutorial code
    public static List<SlotsWrapper> allTutorialSlots;
    public List<SlotsWrapper> allTutorialSlotsP;
    public static int currentTutorial;

    public static bool isTutorial;
    public static Slot currentTutorialSlot;

    //dont need this V
    public static TutorialManager tutorialManager;
    public TutorialManager tutorialManagerP;

    [System.Serializable]
    public struct ChessSlot
    {
        public int index;
        public Piece piece;
        public bool isWhite;
    }

    [System.Serializable]
    public class SlotsWrapper
    {
        public List<ChessSlot> tutorialSlots;
    }

    private void Awake()
    {
        current = this;
        board = chessBoard;
        menus = menuManager;
        defaultBoard = defaultBoardP;
        extraSprites = extraSpritesP;
        allTutorialSlots = allTutorialSlotsP;
        tutorialManager = tutorialManagerP;
    }

    private void Start()
    {
        ResetBoard(false);

        foreach (Slot slot in board.GetComponentsInChildren<Slot>())
        {
            slot.Setup();
        }
    }

    public static Slot GetSlot(int x, int y) //global, for non-slot actions (same as function is the Slot class)
    {
        if (x < 0 || x > 9 || y < 0 || y > 9) return null; //value out of range
        return board.GetChild(x + 10 * y).GetComponent<Slot>();
    }

    public static void ResetBoard(bool updateSlots) //Set board back to its default
    {
        Slot[] slotBoard = board.GetComponentsInChildren<Slot>();
        for (int i = 0; i < defaultBoard.Count; i++) //loop through all slots and set them to the default board counter-part
        {
            if (defaultBoard[i] == null)
            {
                slotBoard[i].piece = null;
            }
            else
            {
                slotBoard[i].piece = Instantiate(defaultBoard[i]);
                if (i > 49) slotBoard[i].piece.isWhite = true; else { slotBoard[i].piece.isWhite = false; }
            }
            if (updateSlots) slotBoard[i].UpdateChanges();
        }
        blackKingSlot = GetSlot(5, 0);
        whiteKingSlot = GetSlot(5, 9);
        ResetToggles();
    }

    public static void ResetToggles() //Gets all color toggle buttons and resets them
    {
        ColorChangeButton[] colorChangeButtons = menus.GetComponentsInChildren<ColorChangeButton>(true);
        foreach (ColorChangeButton colorChange in colorChangeButtons)
        {
            Image image = colorChange.GetComponent<Image>();
            if (image.color != Color.white)
            {
                colorChange.SwitchColor(true);
            }
        }
    }

    public static void SetBoardToTutorial(int index) //Change the board to match the desired tutorial preset
    {
        Slot[] slotBoard = board.GetComponentsInChildren<Slot>();
        List<TutorialManager.ChessSlot> tutorialSlots = tutorialManager.allTutorialSlots[index].tutorialSlots;
        TutorialManager.ChessSlot currentSlot = tutorialSlots[0];
        int slotProgress = 0;
        for (int i = 0; i < slotBoard.Length; i++) //loop through all slots and set them to the desired tutorial board counter-part
        {
            if (i == currentSlot.index)
            {
                currentSlot.piece.isWhite = currentSlot.isWhite;
                slotBoard[i].piece = Instantiate(currentSlot.piece);
                slotProgress++;
                if (slotProgress < tutorialSlots.Count) //out of bounds check
                {
                    currentSlot = tutorialSlots[slotProgress];
                }
            }
            else
            {
                slotBoard[i].piece = null;
            }
            slotBoard[i].UpdateChanges();
        }

        currentTutorial = index;
        tutorialManager.Invoke(nameof(tutorialManager.SetupTutorial), 0.3f);
    }
}
