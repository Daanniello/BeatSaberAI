using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour
{
    public float xInput;
    public float zInput;
    public float movementSpeed;
    public float yInput;

    public NotePositions NotePosition;
    public NoteDirections NoteDirection;

    //private bool isColliding;
    //private bool exitsColliding;

    //private Vector3 enterPoint;
    //private Vector3 exitPoint;

    //private float timeframe;

    void FixedUpdate()
    {           
        transform.position = transform.position + new Vector3(xInput * movementSpeed * Time.deltaTime, yInput * movementSpeed * Time.deltaTime, zInput * movementSpeed * Time.deltaTime);
    }

    public Note SetNotePositionAndDirection(NotePositions notePosition, NoteDirections noteDirection)
    {
        this.NotePosition = notePosition;
        this.NoteDirection = noteDirection;

        if(noteDirection == NoteDirections.down) transform.Rotate(0, 0, 180);
        if (noteDirection == NoteDirections.up) transform.Rotate(0, 0, 0);
        if (noteDirection == NoteDirections.right) transform.Rotate(0, 0, 90);
        if (noteDirection == NoteDirections.left) transform.Rotate(0, 0, 270);
        if (noteDirection == NoteDirections.up_left) transform.Rotate(0, 0, 315);
        if (noteDirection == NoteDirections.up_right) transform.Rotate(0, 0, 45);
        if (noteDirection == NoteDirections.down_left) transform.Rotate(0, 0, 225);
        if (noteDirection == NoteDirections.down_right) transform.Rotate(0, 0, 135);


        return this;
    }

    public void MakeSlashedNote()
    {
        transform.gameObject.GetComponent<BoxCollider>().enabled = false;
    }

    public NoteDirections GetNoteDirection()
    {
        return NoteDirection;
    }

    public enum NotePositions{
        TopLeft,
        TopMiddleLeft,
        TopMiddleRight,
        TopRight,
        MiddleLeft,
        MiddleMiddleLeft,
        MiddleMiddleRight,
        MiddleRight,
        LowerLeft,
        LowerMiddleLeft,
        LowerMiddleRight,
        LowerRight
    }

    public enum NoteDirections
    {
        up = 0,
        down = 1,
        left = 2,
        right = 3,
        up_left = 4,
        up_right = 5,
        down_left = 6,
        down_right = 7,
        none,
        dot,
        
    }

    //void OnTriggerEnter(Collider collider)
    //{
    //    if (isColliding) return;
    //    isColliding = true;
    //    enterPoint = collider.ClosestPointOnBounds(transform.position);        

    //    //Debug.Log($"Trigger enter! {collider.ClosestPointOnBounds(transform.position)}");    
    //}

    //void OnTriggerExit(Collider collider)
    //{
    //    if (exitsColliding) return;
    //    exitsColliding = true;
    //    exitPoint = collider.ClosestPointOnBounds(transform.position);

    //    //Debug.Log($"Trigger exit! {collider.ClosestPointOnBounds(transform.position)}");
    //    collider.gameObject.GetComponent<Saber>().AddPoints(CalculatePoints(enterPoint, exitPoint));
    //    //Debug.Log(CalculatePoints(enterPoint, exitPoint));
    //}

    //public int CalculatePoints(Vector3 enterPoint, Vector3 exitPoint)
    //{        
    //    if (exitPoint == null) return 0;
    //    if (enterPoint.y == exitPoint.y) return 0;
    //    if (enterPoint.y - exitPoint.y >= 1f) return 100;
    //    //Debug.Log(enterPoint.y - exitPoint.y);
    //    return 0;
    //}
}
