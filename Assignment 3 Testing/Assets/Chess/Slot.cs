using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.UI;

//[ExecuteInEditMode]
public class Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Piece piece;
    public int x;
    public int y;

    public Cursor cursor;

    private Image image;
    private Image pieceImage;
    public SlotState state;

    private static new readonly Color light = Color.white;
    private static readonly Color dark = new(0.75f, 0.75f, 0.75f, 1);

    [SerializeField] private bool setWhite; //temporary variable

    // Start is called before the first frame update
    public void Setup() //replacement for start function since this is an inactive object on start (run in ChessBoard singleton)
    {
        pieceImage = transform.GetChild(0).GetComponent<Image>(); //getComponentInChildren returns the image component of this object sadly

        image = GetComponent<Image>();
        int siblingIndex = transform.GetSiblingIndex();

        if (siblingIndex != 0)
        {
            x = siblingIndex % 10;
            y = (int) math.floor(siblingIndex / 10);
        }
        if (piece != null)
        {
            piece = Instantiate(piece);
            piece.isWhite = setWhite;
        }
        ResetColour();
        UpdateChanges();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwapPieces(Slot slot)
    {
        (slot.piece, piece) = (null, slot.piece);
    }
    
    public void UpdateChanges()
    {
        pieceImage.sprite = piece == null ? null : piece.sprite;
        pieceImage.enabled = pieceImage.sprite != null;
        AdjustColour();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ChessBoard.moveInProgress || !TutorialSlot()) return;
        if (!ChessBoard.isRunning)
        {
            if (cursor.currentOption == null && !ChessBoard.isReplacements)
            {
                piece = null;
                UpdateChanges();
                return;
            }
            if (SlotHasKing(this) && !ChessBoard.isReplacements) return;

            if (!ChessBoard.isReplacements)
            {
                piece = Instantiate(cursor.currentOption.piece);
                if (SlotHasKing(this))
                {
                    if (piece.isWhite)
                    {
                        ChessBoard.whiteKingSlot.piece = null;
                        ChessBoard.whiteKingSlot.UpdateChanges();
                        ChessBoard.whiteKingSlot = this;
                    }
                    else
                    {
                        ChessBoard.blackKingSlot.piece = null;
                        ChessBoard.blackKingSlot.UpdateChanges();
                        ChessBoard.blackKingSlot = this;
                    }
                }
            }
            else if (state == SlotState.placeable)
            {
                piece = Instantiate(cursor.currentOption.piece);
                piece.isWhite = y > 4;
            }
            UpdateChanges();
            return;
        }
        if (cursor.selectedSlot == this)
        {
            PutPieceBackDown();
        }
        else if ((cursor.selectedSlot == null || cursor.selectedSlot != this) && piece != null && piece.isWhite == ChessBoard.isWhiteTurn)
        {
            Debug.Log("picked up");
            PutPieceBackDown();
            cursor.SetSlot(this);

            //Gets all possible moves and displays them
            cursor.validSlots = GetValidSquares();
            foreach (Slot slot in cursor.validSlots)
            {
                slot.image.color = Color.green;
                slot.state = SlotState.moveable;
            }

            List<Slot> actionSquares = GetActionSquares();
            cursor.validSlots.AddRange(actionSquares);
            foreach (Slot slot in actionSquares)
            {
                slot.image.color = Color.red;
                slot.state = SlotState.activatable;
            }
        }
        else if (state == SlotState.moveable) //make new section that handles when cursor's selected slot is this
        {
            state = SlotState.moveable;
            Debug.Log("placed down");
            if (cursor.selectedSlot != this && (piece == null || cursor.selectedSlot.piece.type != PieceType.necromancer)) //allow for necromancer re-summon
            {
                ChessBoard.isWhiteTurn = !ChessBoard.isWhiteTurn;
            }

            cursor.MoveBetweenSlots(cursor.selectedSlot, this);

            SwapPieces(cursor.selectedSlot);
            cursor.selectedSlot.UpdateChanges();
        }
        else if (state == SlotState.activatable)
        {
            Debug.Log("ability used");
            ChessBoard.isWhiteTurn = !ChessBoard.isWhiteTurn;

            switch (cursor.selectedSlot.piece.type)
            {
                case PieceType.cannon:
                    cursor.MoveBetweenSlots(cursor.selectedSlot, this);
                    piece = null; //delete attacked enemy
                    cursor.GetComponent<Image>().sprite = ChessBoard.extraSprites[0];
                    break;
                case PieceType.necromancer:
                    piece.isWhite = !piece.isWhite;
                    GenerateParticles(cursor.particleSystems[2], piece.isWhite ? Color.white : Color.black);
                    UpdateChanges();
                    TakenSlotKingCheck(this);
                    break;
            }
        }

        if (state != SlotState.normal)
        {
            Debug.Log("adjusted");
            cursor.selectedSlot = null;
            cursor.ClearPossibleMoves();
        }
    }

    public void GenerateParticles(ParticleSystem particleSystem, Color color)
    {
        ParticleSystem system = Instantiate(particleSystem);
        ParticleSystem.MainModule ma = system.main;

        system.transform.position = transform.position;
        ma.startColor = color;
        system.Play();
        Destroy(system.gameObject, ma.startLifetime.constant);
    }

    public void GenerateDeathParticles(Piece piece)
    {
        if (piece != null)
        {
            ParticleSystem.MainModule ma;
            ParticleSystem system;
            if (piece.type == PieceType.king || piece.type == PieceType.necromancer)
            {
                system = Instantiate(cursor.particleSystems[1]);
            }
            else
            {
                system = Instantiate(cursor.particleSystems[0]);
            }
            ma = system.main;

            system.transform.position = transform.position;
            ma.startColor = piece.isWhite ? Color.white : Color.black;
            system.Play();
            Destroy(system.gameObject, 10);
        }
    }

    public void PutPieceBackDown()
    {
        cursor.selectedSlot = null;
        cursor.ClearPossibleMoves();
    }

    public void PieceExtraAbility(Slot cursorSlot) //runs after swap
    {
        if (piece.type == PieceType.piercer)
        {
            if (math.abs(x - cursorSlot.x) < 2) return; //did not move 2 spaces
            Slot slot = GetSlot((x + cursorSlot.x) / 2, (y + cursorSlot.y) / 2); //average slot (slot in middle of target and start)
            if (CanTake(slot))
            {
                TakenSlotKingCheck(slot);
                slot.GenerateDeathParticles(slot.piece);
                slot.piece = null;
            }
            slot.ResetColour();
            slot.UpdateChanges();
        }
        else if (piece.type == PieceType.juggernaut)
        {
            List<Slot> tempResult = new()
            {
                GetSlot(x - 1, y), //left
                GetSlot(x, y - 1), //up
                GetSlot(x + 1, y), //right
                GetSlot(x, y + 1) //down
            };
            foreach (Slot slot in tempResult)
            {
                if (CanTake(slot))
                {
                    TakenSlotKingCheck(slot);
                    slot.GenerateDeathParticles(slot.piece);
                    slot.piece = null;
                    slot.ResetColour();
                    slot.UpdateChanges();
                }
            }
        }
        /*else if (piece.type == PieceType.necromancer)
        {
            if (takenPiece != null)
            {
                cursor.storedPiece = takenPiece;
                List<Slot> validSlots = GetValidSquares();
                foreach (Slot slot in validSlots)
                {
                    if (slot.piece == null)
                    {
                        slot.image.color = Color.blue;
                        slot.state = SlotState.special;
                        cursor.validSlots.Add(slot);
                    }
                }
            }
        }*/
    }

    public bool SlotHasKing(Slot slot)
    {
        return slot.piece != null && (slot.piece.type == PieceType.king || slot.piece.type == PieceType.necromancer);
    }

    public void TakenSlotKingCheck(Slot slot)
    {
        if (SlotHasKing(slot))
        {
            ChessBoard.menus.SetMenu(3);
            ChessBoard.menus.SetGameOver(piece.isWhite ? "Black" : "White");
            ChessBoard.isRunning = false;
        }
    }

    public void AdjustColour()
    {
        if (piece != null)
        {
            //pieceImage.enabled = true;
            pieceImage.color = piece.isWhite ? Color.white : Color.gray;
        }
        else
        {
            //pieceImage.enabled = false;
        }
    }

    public void ResetColour()
    {
        state = SlotState.normal;
        if (y % 2 == 0)
        {
            image.color = x % 2 == 0 ? dark : light;
        }
        if (y % 2 == 1)
        {
            image.color = x % 2 == 0 ? light : dark;
        }
    }

    public Slot GetSlot(int x, int y)
    {
        if (x < 0 || x > 9 || y < 0 || y > 9) return null; //value out of range
        return transform.parent.GetChild(x + 10 * y).GetComponent<Slot>();
    }

    public bool CanTake(Slot slot) //help function (checks if a slot could be taken by the piece on this slot)
    {
        return slot != null && (slot.piece == null || piece.isWhite != slot.piece.isWhite);
    }

    public List<Slot> GetValidSquares() //all piece movement patterns
    {
        List<Slot> result = new();
        if (piece == null)
        {
            return null; //something went wrong
        }
        else if (piece.type == PieceType.pawn)
        {
            int addY = piece.isWhite ? -1 : 1;
            Slot slot1 = GetSlot(x, y + addY);
            if (slot1 != null && slot1.piece == null)
            {
                result.Add(slot1);
                Slot slot2 = GetSlot(x, y + addY * 2);
                if (slot2 != null && slot2.piece == null)
                {
                    result.Add(slot2);
                }
            }
            for (int i = -1; i < 2; i += 2)
            {
                Slot slot = GetSlot(x + i, y + addY);
                if (slot != null && slot.piece != null && piece.isWhite != slot.piece.isWhite) //must be a piece in that slot
                {
                    result.Add(slot);
                }
            }
        }
        else if (piece.type == PieceType.knight)
        {
            List<Slot> tempResult = new() //sadly spamming functions is the most effecient (and least messy) way to do this
            {
                GetSlot(x - 2, y - 1), //Left-up
                GetSlot(x - 2, y + 1), //Left-down
                GetSlot(x - 1, y - 2), //Up-left
                GetSlot(x + 1, y - 2), //Up-right
                GetSlot(x + 2, y - 1), //Right-up
                GetSlot(x + 2, y + 1), //Right-down
                GetSlot(x - 1, y + 2), //Down-left
                GetSlot(x + 1, y + 2), //Down-right
            };
            foreach (Slot slot in tempResult)
            {
                if (CanTake(slot))
                {
                    result.Add(slot);
                }
            }
        }
        else if (piece.type == PieceType.bishop || piece.type == PieceType.queen) //piecetype queen shortcut
        {
            for (int xC = -1; xC < 2; xC += 2)
            {
                for (int yC = -1; yC < 2; yC += 2)
                {
                    result.AddRange(GetSlotsInLine(xC, yC)); //loops switch diagonals
                }
            }
        }
        if (piece.type == PieceType.rook || piece.type == PieceType.queen) //piecetype queen shortcut (can't be else if)
        {
            result.AddRange(GetSlotsInLine(-1, 0)); //left
            result.AddRange(GetSlotsInLine(0, -1)); //up
            result.AddRange(GetSlotsInLine(1, 0)); //right
            result.AddRange(GetSlotsInLine(0, 1)); //down
        }
        else if (piece.type == PieceType.king || piece.type == PieceType.juggernaut)
        {
            for (int xC = -1; xC < 2; xC++)
            {
                for (int yC = -1; yC < 2; yC++)
                {
                    Slot slot = GetSlot(x + xC, y + yC);
                    if (CanTake(slot))
                    {
                        result.Add(slot);
                    }
                }
            }
        }
        //CUSTOM PIECES
        else if (piece.type == PieceType.cannon || piece.type == PieceType.necromancer)
        {
            List<Slot> tempResult = new()
            {
                GetSlot(x - 1, y), //left
                GetSlot(x, y - 1), //up
                GetSlot(x + 1, y), //right
                GetSlot(x, y + 1) //down
            };
            foreach (Slot slot in tempResult)
            {
                if (slot != null && slot.piece == null)
                {
                    result.Add(slot);
                }
            }
        }
        else if (piece.type == PieceType.piercer)
        {
            for (int xC = -1; xC < 2; xC += 2)
            {
                for (int yC = -1; yC < 2; yC += 2)
                {
                    Slot firstSlot = GetSlot(x + xC, y + yC);
                    Slot secondSlot = GetSlot(x + xC * 2, y + yC * 2);
                    if (CanTake(secondSlot))
                    {
                        result.Add(secondSlot);
                    }
                    if (CanTake(firstSlot))
                    {
                        result.Add(firstSlot);
                    }
                }
            }
        }
        else if (piece.type == PieceType.doubleKnight)
        {
            List<Slot> tempResult = new() //sadly spamming functions is the most effecient (and least messy) way to do this
            {
                GetSlot(x - 3, y - 1), //Left-up
                GetSlot(x - 3, y + 1), //Left-down
                GetSlot(x - 1, y - 3), //Up-left
                GetSlot(x + 1, y - 3), //Up-right
                GetSlot(x + 3, y - 1), //Right-up
                GetSlot(x + 3, y + 1), //Right-down
                GetSlot(x - 1, y + 3), //Down-left
                GetSlot(x + 1, y + 3), //Down-right
            };
            foreach (Slot slot in tempResult)
            {
                if (CanTake(slot))
                {
                    result.Add(slot);
                }
            }
        }
        return result;
    }

    private List<Slot> GetActionSquares() //special movement patterns
    {
        List<Slot> result = new();
        if (piece == null)
        {
            return null;
        }
        else if (piece.type == PieceType.cannon)
        {
            result.AddRange(GetSlotsInLine(-1, 0, true)); //left
            result.AddRange(GetSlotsInLine(0, -1, true)); //up
            result.AddRange(GetSlotsInLine(1, 0, true)); //right
            result.AddRange(GetSlotsInLine(0, 1, true)); //down
        }
        else if (piece.type == PieceType.necromancer)
        {
            List<Slot> tempResult = new()
            {
                GetSlot(x - 1, y), //left
                GetSlot(x, y - 1), //up
                GetSlot(x + 1, y), //right
                GetSlot(x, y + 1) //down
            };
            foreach (Slot slot in tempResult)
            {
                if (CanTake(slot) && slot.piece != null)
                {
                    result.Add(slot);
                }
            }
        }
        return result;
    }

    public List<Slot> GetSlotsInLine(int xC, int yC, bool onlyEnd = false) //helper function
    {
        List<Slot> result = new();
        Slot currentslot = this;
        while (currentslot != null)
        {
            currentslot = GetSlot(currentslot.x + xC, currentslot.y + yC);
            if (currentslot == null) break;
            if (currentslot.piece == null)
            {
                if (onlyEnd) continue; //bad method
                result.Add(currentslot);
            }
            else
            {
                if (currentslot.piece.isWhite != piece.isWhite) result.Add(currentslot); //allow taking of last piece in line if it is takeable
                break;
            }
        }
        return result;
    }

    public void MovePieceToSlot(Slot endSlot)
    {
        cursor.SetSlot(this);
        cursor.MoveBetweenSlots(this, endSlot);
        endSlot.piece = piece;
        piece = null;
        UpdateChanges();
    }
    
    private bool TutorialSlot()
    {
        if (!ChessBoard.isTutorial) return true;

        if (ChessBoard.currentTutorialSlot != this) 
        {
            ChessBoard.tutorialManager.text.text = "Wrong Move!";
            ChessBoard.tutorialManager.CancelInvoke(nameof(ChessBoard.tutorialManager.ReSetText));
            ChessBoard.tutorialManager.Invoke(nameof(ChessBoard.tutorialManager.ReSetText), 1);
            return false; 
        }

        //ChessBoard.tutorialManager.Invoke(nameof(ChessBoard.tutorialManager.NextStep), 0.4f);
        if (ChessBoard.tutorialManager.readyForNextMove)
        {
            ChessBoard.tutorialManager.NextStep();
            return true;
        }

        return false;
    }

    //tile hover code
    readonly float colorChange = -0.1f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = new Color(image.color.r + colorChange, image.color.g + colorChange, image.color.b + colorChange);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = new Color(image.color.r - colorChange, image.color.g - colorChange, image.color.b - colorChange);
    }
    /*
public bool IsValidMove()
{
if (piece != null)
{
  return false;
}
if (piece.type == PieceType.pawn)
{
  //pawn mechanics
}
return true; //will be return false once all piece mechanics are done
}*/

}
public enum SlotState
{
    normal = 0,
    moveable = 1 << 0,
    activatable = 1 << 1,
    special = 1 << 2,
    placeable = 1 << 3
}