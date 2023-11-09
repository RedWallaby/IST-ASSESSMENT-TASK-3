using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReplacementOption : MonoBehaviour, IPointerClickHandler
{
    public TMP_Text text;
    public Image image;
    public Piece piece;

    public Cursor cursor;

    void Start()
    {
        piece = Instantiate(piece);
        piece.isWhite = true;
        image.sprite = piece.sprite;
    }

    public void OnPointerClick(PointerEventData eventData) //manage clicks for replacements
    {
        cursor.ClearPossibleMoves();

        Image cursorImage = cursor.GetComponent<Image>();

        if (cursor.currentOption == this) //de-select
        {
            cursorImage.sprite = null;
            cursor.currentOption = null;
            cursorImage.enabled = false;
            return;
        }

        cursor.currentOption = this;
        cursorImage.sprite = piece.sprite;
        cursorImage.enabled = true;

        if (ChessBoard.isReplacements) //manage placeable slots for selected piece
        {
            cursor.validSlots = GetPiecesSlots(piece);
            foreach (Slot slot in cursor.validSlots)
            {
                slot.GetComponent<Image>().color = Color.cyan;
                slot.state = SlotState.placeable;
            }
        }
        else
        {
            cursorImage.color = piece.isWhite ? Color.white : new Color(0.75f, 0.75f, 0.75f);
        }
    }

    public List<Slot> GetPiecesSlots(Piece piece) //get the replacment slots for every piece
    {
        int y = piece.isWhite ? 9 : 0;
        List<int> xs = new();
        PieceType type = piece.type;
        if (type == PieceType.pawn)
        {
            //nothing
            return new List<Slot>();
        }
        else if (type == PieceType.rook || type == PieceType.cannon)
        {
            xs.Add(1);
            xs.Add(8);
        }
        else if (type == PieceType.knight || type == PieceType.doubleKnight)
        {
            xs.Add(2);
            xs.Add(7);
        }
        else if (type == PieceType.bishop || type == PieceType.piercer)
        {
            xs.Add(3);
            xs.Add(6);
        }
        else if (type == PieceType.queen || type == PieceType.juggernaut)
        {
            xs.Add(4);
        }
        else if (type == PieceType.king || type == PieceType.necromancer)
        {
            xs.Add(5);
        }

        List<Slot> slots = new();
        for (y = 0; y < 10; y += 9)
        {
            foreach (int x in xs)
            {
                slots.Add(ChessBoard.GetSlot(x, y));
            }
            if (xs.Count > 1) //not pawn, queen, or king variant 
            {
                slots.Add(ChessBoard.GetSlot(0, y));
                slots.Add(ChessBoard.GetSlot(9, y));
            }
        }
        
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].piece.type == piece.type)
            {
                slots.RemoveAt(i);
                i--;
            }
        }
        return slots;
    }

    public void ChangePieceColor()
    {
        piece.isWhite = !piece.isWhite;
        image.sprite = piece.isWhite ? piece.sprite : piece.sprite2;
    }
}
