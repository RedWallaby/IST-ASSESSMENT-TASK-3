using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class Cursor : MonoBehaviour
{
    public Slot selectedSlot;

    public List<Slot> validSlots = new();

    public Piece storedPiece; //for necromancer

    public ReplacementOption currentOption;

    private bool isLerping;
    private float lerpAmount;
    private Slot startSlot;
    private Slot endSlot;
    private Piece placedPiece;
    private Piece pieceTaken;

    public List<ParticleSystem> particleSystems = new();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isLerping)
        {
            if (lerpAmount > 1)
            {
                endSlot.GenerateDeathParticles(pieceTaken);
                isLerping = false;
                ChessBoard.moveInProgress = false;
                if (startSlot.piece == null/* && startSlot.piece.type != PieceType.cannon*/) //if a piece moved (cannot be cannon)
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
            transform.position = Input.mousePosition;
        }
        /*if (!ChessBoard.isRunning && currentOption != null)
        {
            GetComponent<Image>().color = transform.localPosition.y < 0 ? Color.white : Color.grey;
        }*/
    }

    /*public void ParticleHandler()
    {
        if (pieceTaken != null)
        {
            ParticleSystem.MainModule ma;
            ParticleSystem system;
            if (pieceTaken.type == PieceType.king || pieceTaken.type == PieceType.necromancer)
            {
                system = kingExplosion;
            }
            else
            {
                system = pieceExplosion;
            }
            ma = system.main;

            system.transform.position = transform.position;
            ma.startColor = pieceTaken.isWhite ? Color.white : Color.black;
            system.Play();
        }
    }*/

    public void CheckEndOfGame()
    {
        if (ChessBoard.isTutorial == false && selectedSlot != this && pieceTaken != null && (pieceTaken.type == PieceType.king || pieceTaken.type == PieceType.necromancer)) //check for ending game
        {
            ChessBoard.menus.SetMenu(3);
            ChessBoard.menus.SetGameOver(pieceTaken.isWhite ? "Black" : "White");
            ChessBoard.isRunning = false;
        }
    }

    public void MoveBetweenSlots(Slot slot1, Slot slot2)
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

    public void SetSlot(Slot slot)
    {
        Image image = GetComponent<Image>();
        if (slot == null)
        {
            //image.enabled = false;
            //image.sprite = null;
            selectedSlot = slot;
        }
        else
        {
            image.sprite = slot.piece.sprite;
            image.color = slot.piece.isWhite ? Color.white : Color.grey;
            selectedSlot = slot;
        }
    }

    public void ClearPossibleMoves()
    {
        foreach (Slot slot in validSlots)
        {
            slot.ResetColour();
            //slot.AdjustColour();
        }
        validSlots.Clear();
    }

    public void ResetCursor()
    {
        ClearPossibleMoves();
        //Image image = GetComponent<Image>();
        //image.sprite = null;
        //image.enabled = false;
        selectedSlot = null;
        storedPiece = null;
        currentOption = null;
    }

    public void HideCursor()
    {
        Image image = GetComponent<Image>();
        image.sprite = null;
        image.enabled = false;
    }
}
