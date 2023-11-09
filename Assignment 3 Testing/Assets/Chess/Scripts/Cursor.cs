using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class Cursor : MonoBehaviour
{
    public Slot selectedSlot;

    public List<Slot> validSlots = new();

    public ReplacementOption currentOption;

    private bool isLerping;
    private float lerpAmount;
    private Slot startSlot;
    private Slot endSlot;
    private Piece pieceTaken;

    public List<ParticleSystem> particleSystems = new();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isLerping) //if it is moving a piece between two points
        {
            if (lerpAmount > 1) //destination reached
            {
                endSlot.GenerateDeathParticles(pieceTaken);
                isLerping = false;
                ChessBoard.moveInProgress = false;
                if (startSlot.piece == null) //if a piece moved (cannot be cannon)
                {
                    endSlot.PieceExtraAbility(startSlot);
                }
                startSlot.UpdateChanges();
                endSlot.UpdateChanges();
                GetComponent<Image>().enabled = false;
                CheckEndOfGame();
            }
            transform.position = new Vector2(math.lerp(startSlot.transform.position.x, endSlot.transform.position.x, lerpAmount), math.lerp(startSlot.transform.position.y, endSlot.transform.position.y, lerpAmount));
            lerpAmount += 10f * Time.deltaTime;
        }
        else
        {
            Vector3 vector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            vector.z = 10;
            transform.position = vector;
        }
    }

    public void CheckEndOfGame() //same as a slots checking for the end of the game, except relative to this class (only used as a helper function)
    {
        if (ChessBoard.isTutorial == false && selectedSlot != this && pieceTaken != null && (pieceTaken.type == PieceType.king || pieceTaken.type == PieceType.necromancer)) //check for ending game
        {
            ChessBoard.menus.SetMenu(3);
            ChessBoard.menus.SetGameOver(pieceTaken.isWhite ? "Black" : "White");
            ChessBoard.isRunning = false;
        }
    }

    public void MoveBetweenSlots(Slot slot1, Slot slot2) //setup and activate the moving from slot1 to slot2
    {
        GetComponent<Image>().enabled = true;
        transform.position = slot1.transform.position;
        lerpAmount = 0;
        isLerping = true;
        ChessBoard.moveInProgress = true;
        startSlot = slot1;
        endSlot = slot2;
        if (slot2.piece != null)
        {
            pieceTaken = Instantiate(slot2.piece);
        }
        else
        {
            pieceTaken = null;
        }
    }

    public void SetSlot(Slot slot) //sets the cursor's reference slot (the slot you currently have selected)
    {
        Image image = GetComponent<Image>();
        image.sprite = slot.piece.sprite;
        image.color = slot.piece.isWhite ? Color.white : Color.grey;
        selectedSlot = slot;
    }

    public void ClearPossibleMoves() //remove green/red/cyan squares from the board
    {
        foreach (Slot slot in validSlots)
        {
            slot.ResetBaseColour();
        }
        validSlots.Clear();
    }

    public void ResetCursor()
    {
        ClearPossibleMoves();
        selectedSlot = null;
        currentOption = null;
    }

    public void HideCursor()
    {
        Image image = GetComponent<Image>();
        image.sprite = null;
        image.enabled = false;
    }
}
