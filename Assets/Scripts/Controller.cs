using System;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public enum State
    {
        serverable,
        hittable,
        hidden
    }
    public Canvas serverText;
    public Animator animator;
    [SerializeField] protected int playerNum = 1; // Default to Player 1
    private Vector3 player1Position = new Vector3(1.52f, 0.65f, -14.21f);
    private Vector3 player2Position = new Vector3(-6.63f, 0.65f, 28.2f);
    public Vector3 defaultPosition
    {
        get { return playerNum == 1 ? player1Position : player2Position; }
    }
    [NonSerialized] public bool isHitting, isServing;
    [NonSerialized] public float horizontalVelocity, verticalVelocity;  // Changed to public

    #region Court Size
    [NonSerialized] public float netZ = 6.11f;
    [NonSerialized] public float minX = -13.75f, maxX = 8.0f, minZ = -16.04f, maxZ = 31.05f;
    #endregion
    [NonSerialized] public State _state = State.serverable;

    public void setServerText(bool b)
    {
        serverText.gameObject.SetActive(b);
    }
}
