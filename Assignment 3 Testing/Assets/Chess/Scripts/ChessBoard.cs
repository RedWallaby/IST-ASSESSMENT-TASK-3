using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.Mathematics;
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
    public static PieceType whiteQueenEQ;
    public static PieceType blackQueenEQ;

    public static List<Piece> defaultBoard;
    public List<Piece> defaultBoardP;

    public static List<Sprite> extraSprites;
    public List<Sprite> extraSpritesP;

    public static List<Piece> queens;
    public List<Piece> queensP;

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
        ai = aiP;
        queens = queensP;
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
        ai.ResetColors();
    }

    public static void WipeBoard()
    {
        Slot[] slotBoard = board.GetComponentsInChildren<Slot>();
        for (int i = 0; i < defaultBoard.Count; i++) //loop through all slots and set them to the default board counter-part
        {
            slotBoard[i].piece = null;
            slotBoard[i].UpdateChanges();
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


    //AI
    public static AI ai;
    public AI aiP;

    public static Stack<List<AISlot>> previousSlots = new();

    public static AISlot[] currentGame = new AISlot[100];

    /*public static void MoveWithoutVisual(AISlot start, AISlot end) //Moves a piece without updating the board and records the changes of the move
    {
        List<AISlot> slots = new()
        {
            CreateUndo(end)
        };
        //print(start.x + " " + start.y);
        //if (GetSlot(start.x, start.y).piece != null) print(GetSlot(start.x, start.y).piece.type);
        
        switch (start.piece.type)
        {
            case PieceType.cannon:
                end.piece = null;
                break;
            case PieceType.necromancer:
                end.piece.isWhite = !end.piece.isWhite;
                break;
            default:
                (start.piece, end.piece) = (null, start.piece);
                slots.AddRange(ExtraAbilityWithoutVisual(start, end));
                slots.Add(CreateUndo(start));
                break;
        }
        previousSlots.Push(slots);
    }*/

    public static void MoveWithoutVisual(AISlot start, AISlot end) //Moves a piece without updating the board and records the changes of the move
    {
        List<AISlot> slots = new()
        {
            CreateUndo(end)
        };
        /*print(start.x + " " + start.y);
        if (GetSlot(start.x, start.y).piece != null) print(GetSlot(start.x, start.y).piece.type);*/

        if (end.piece != null)
        {
            if (start.piece.type == PieceType.cannon)
            {
                end.piece = null;
                previousSlots.Push(slots);
                return;
            }
            else if (start.piece.type == PieceType.necromancer)
            {
                end.piece.isWhite = !end.piece.isWhite;
                previousSlots.Push(slots);
                return;
            }
        }
        /*if (start.piece == null)
        {
            print(start.x + " " + start.y);
        }*/
        slots.Add(CreateUndo(start));
        (start.piece, end.piece) = (null, start.piece);
        slots.AddRange(ExtraAbilityWithoutVisual(start, end));
        previousSlots.Push(slots);
    }

    public static List<AISlot> ExtraAbilityWithoutVisual(AISlot start, AISlot end)
    {
        List<AISlot> slots = new();
        /*if (end.piece == null)
        {
            print("how...");
        }*/
        if (end.piece.type == PieceType.pawn)
        {
            if (end.y == 0 || end.y == 9)
            {
                end.piece.type = PieceType.queen;
            }
        }
        if (end.piece.type == PieceType.piercer)
        {
            if (math.abs(end.x - start.x) < 2) return slots; //did not move 2 spaces
            AISlot slot = GetSlotInGame((end.x + start.x) / 2, (end.y + start.y) / 2); //average slot (slot in middle of target and start)
            if (!end.CanTake(slot)) return slots;
            slots.Add(CreateUndo(slot));
            slot.piece = null;
        }
        else if (end.piece.type == PieceType.juggernaut) //issue for juggernaut is here (fixed)
        {
            List<AISlot> tempResult = new()
            {
                GetSlotInGame(end.x - 1, end.y), //left
                GetSlotInGame(end.x, end.y - 1), //up
                GetSlotInGame(end.x + 1, end.y), //right
                GetSlotInGame(end.x, end.y + 1) //down
            };
            foreach (AISlot slot in tempResult)
            {
                if (end.CanTake(slot) && slot != start) //adding this second statement fixes this V - Fixed differently by moving slots.Add(CreateUndo(start)) in front of this function             nevermind
                {
                    slots.Add(CreateUndo(slot)); //creates an undo on the start position and overrides that which simply put - bad
                    slot.piece = null;
                }
            }
        }
        return slots;
    }

    public static List<AI.Move> GetAllMoves(bool isMaximising)
    {
        List<AI.Move> result = new();
        foreach (AISlot slot in currentGame)
        {
            if (slot.piece == null || slot.piece.isWhite != isMaximising) continue;
            List<AISlot> currentSlots = slot.GetValidSquares();
            currentSlots.AddRange(slot.GetActionSquares());
            foreach (AISlot slot1 in currentSlots)
            {
                AI.Move move;
                move.start = slot;
                move.end = slot1;
                result.Add(move);
            }
        }
        return result;
    }

    public static void UndoMove()
    {
        List<AISlot> currentSlots = previousSlots.Pop();
        foreach (AISlot pos in currentSlots)
        {
            AISlot slot = GetSlotInGame(pos.x, pos.y);
            slot.piece = pos.piece;
        }
    }

    public static AISlot GetSlotInGame(int x, int y)
    {
        if (x < 0 || x > 9 || y < 0 || y > 9) return null; //value out of range
        return currentGame[x + 10 * y];
    }

    public static AISlot CreateUndo(AISlot slot)
    {
        AISlot pos;
        pos = new()
        {
            x = slot.x,
            y = slot.y,
            piece = new()
        };
        if (slot.piece != null) //necessary
        {
            pos.piece.type = slot.piece.type;
            pos.piece.isWhite = slot.piece.isWhite;
        }
        else
        {
            pos.piece = null;
        }
        return pos;
    }
}
