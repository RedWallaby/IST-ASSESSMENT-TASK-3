using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FallingPiece : MonoBehaviour
{
    public float xMove;
    private float yMove;
    private float direction;

    public float xWidthR;
    public float yWidthR;

    public List<Piece> pieces;

    void Start()
    {
        Restart();
    }

    void Restart()
    {
        direction = 0;
        Piece randomPiece = pieces[Random.Range(0, pieces.Count)];
        bool isWhite = Random.value > 0.5;
        GetComponent<Image>().sprite = isWhite ? randomPiece.sprite : randomPiece.sprite2;
        transform.localPosition = new Vector3(Random.Range(-xWidthR, xWidthR), yWidthR + 100);
        xMove = transform.localPosition.x < 0 ? Random.Range(0f, 1f) : Random.Range(-1f, 0f);
        yMove = Random.Range(-1f, -2f);
    }

    void Update()
    {
        transform.localPosition += 100 * Time.deltaTime * new Vector3(xMove, yMove);
        direction += 50 * Time.deltaTime;
        transform.eulerAngles = Vector3.forward * direction;
        if (transform.localPosition.y < -yWidthR - 100)
        {
            Restart();
        }
    }
}
