using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Saber : MonoBehaviour
{
    public float id;

    public float rotationSpeed;//Rotation multiplier
    public float positionSpeed;//Position multiplier

    public NeuralNetworkDFF network;

    public int totalPoints;

    public LayerMask layer;

    private Vector3 previousPos;
    private Quaternion previousRot;

    private Vector3 previousSaberTipPos;

    private float energy;

    private bool goalIsReached = false;

    private int pointsForStayingAlive = 0;

    private Manager manager;

    private bool hasNoteBeenHitCorrectly = false;
    private Vector3 lastHitPosition;

    private bool angleReached = false;
    private Note.NoteDirections UpcommingNoteDirection;
    private Note.NoteDirections LastSlashedNoteDirection = Note.NoteDirections.none;
    private bool SaberRanOutOfEnergy = false;

    void Start()
    {
        manager = FindObjectOfType(typeof(Manager)) as Manager;
        energy = manager.SaberEnergy;
    }

    void FixedUpdate()
    {
        //If the saber runs out of energy, it needs to learn more 
        if (SaberRanOutOfEnergy) return;
        if (IsOutOfEnergy())
        {
            NeedToLearnMore();
            SaberRanOutOfEnergy = true;
            GiveRewards(pointsForStayingAlive / 10);
            return;
        }

        pointsForStayingAlive++;
        

        //Checks if the BeforeAngle has been correctly
        RaycastHit NoteCheckHit;
        if (Physics.Raycast(transform.position, new Vector3(0.0f, 0.0f, -1.0f), out NoteCheckHit, Mathf.Infinity, layer))
        {
            Note noteChecked = NoteCheckHit.transform.gameObject.GetComponent<Note>();
            UpcommingNoteDirection = noteChecked.NoteDirection;
            RestrictionForNoteBeforeSwingCheck(manager.BeforeAngleDifficulty);
        }

        //Check if the last note hit was also with a angle after the hit 
        if (hasNoteBeenHitCorrectly)
        {
            if (manager.AfterAngleIsNeeded)
            {
                RestrictionForNoteAfterSwingCheck(manager.AfterAngleDifficulty);
            }
            else
            {
                Reset();               
            }
                 
        }


        //Check if the saber is hitting a note 
        if (manager.DebugsEnabled) Debug.DrawRay(transform.Find("Saber").position, transform.forward * -2, Color.yellow);
        RaycastHit hitAngle;
        if (Physics.Raycast(transform.Find("Saber").position, transform.forward * -1, out hitAngle, 1, layer))
        {
            var note = hitAngle.transform.gameObject.GetComponent<Note>();
            note.MakeSlashedNote();
            var saberTip = transform.Find("SaberTip");

            if (angleReached)
            {
                //Checks if the note is hit on the right side & if the swing was forwards
                RestrictionsForNoteDirectionCheck(saberTip);
            }
            else
            {
                angleReached = false;
            }
            Destroy(hitAngle.transform.gameObject);
        }

        previousSaberTipPos = transform.Find("SaberTip").position;
        previousRot = transform.rotation;
        previousPos = transform.position;

        //Check if the Saber AI has reached the total points needed
        //IF NOT: It needs to learn more
        if (totalPoints < manager.TotalPointsNeeded)
        {
            var output = NeedToLearnMore();
            //Debug.Log($"up: {output[0]} down: {output[1]} right: {output[2]} left:{output[3]}");

            //Output 0: swing up direction if note is close 
            //Output 1: swing down direction if note is close 
            //Output 2: swing right direction if note is close 
            //Output 3: swing left direction if note is close 
            //Output 4: Dont swing if note is far away 
            var xAsRotation = manager.SaberXAsRotationEnabled ? output[0] * 90 : 0;
            var yAsRotation = manager.SaberYAsRotationEnabled ? output[1] * 90 : 0;
            var zAsRotation = manager.SaberZAsRotationEnabled ? output[2] * 90 : 0;
            transform.Rotate(xAsRotation * Time.deltaTime, yAsRotation * Time.deltaTime, zAsRotation * Time.deltaTime);
            //Debug.Log(output[2]);
            var positionChange = manager.SaberPostitionEnabled ? this.transform.right * output[3] : new Vector3(0, 0, 0);
            transform.position += positionChange;
        }
        else
        {
            if (!goalIsReached)
            {
                if (manager.DebugsEnabled) Debug.Log("Goal Reached!");
                
                goalIsReached = true;
            }            
        }
    }

    private void Reset()
    {
        hasNoteBeenHitCorrectly = false;
        angleReached = false;
        LastSlashedNoteDirection = Note.NoteDirections.none;
    }

    public float[] NeedToLearnMore()
    {
        float[] input = new float[manager.GetLayers()[0]];//input to the neural network   

        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, new Vector3(0.0f, 0.0f, -1.0f), out hit, 20, layer))
        {
            input[0] = (1f / 8f) * ((float)UpcommingNoteDirection + 1f);           

            //if (UpcommingNoteDirection == Note.NoteDirections.up) input[0] = 1 / hit.distance;
            //if (UpcommingNoteDirection == Note.NoteDirections.down) input[0] = 1 / hit.distance;
            //if (UpcommingNoteDirection == Note.NoteDirections.right) input[0] = 1 / hit.distance;
            //if (UpcommingNoteDirection == Note.NoteDirections.left) input[0] = 1 / hit.distance;
            //if (UpcommingNoteDirection == Note.NoteDirections.up_left) input[0] = 1 / hit.distance;
            //if (UpcommingNoteDirection == Note.NoteDirections.up_right) input[5] = 1 / hit.distance;
            //if (UpcommingNoteDirection == Note.NoteDirections.down_left) input[6] = 1 / hit.distance;
            //if (UpcommingNoteDirection == Note.NoteDirections.down_right) input[7] = 1 / hit.distance;

            input[1] = 1 / ((hit.distance * 0.5) < 1 ? 1 : hit.distance);
            
            if (manager.DebugsEnabled) Debug.DrawRay(transform.position, new Vector3(0.0f, 0.0f, -1.0f) * hit.distance, Color.yellow);
        }
        else
        {
            if(manager.DebugsEnabled) Debug.DrawRay(transform.position, new Vector3(0.0f, 0.0f, -1.0f) * 1000, Color.white);
            input[0] = 0;
            input[1] = 0;//if nothing is detected, will return 0 to network                
        }


        var energyInput = energy == 0 ? 0f : (1f / (float) manager.SaberEnergy) * energy;
        input[2] = energyInput;

        var rotationX = transform.rotation.x;
        var rotationY = transform.rotation.y;
        var rotationZ = transform.rotation.z;
        
        //Debug.Log($"input: {transform.rotation.x} | {transform.rotation.y} | {transform.rotation.z}");

        input[3] = rotationX;
        input[4] = rotationY;
        input[5] = rotationZ;
       

        //Debug.Log($"up: {input[0]} down: {input[1]} right: {input[2]} left: {input[3]} none: {input[4]}");


        //ID is for debugging 
        bool shouldLogNeuralNetwork = id == 1 ? true : false;
        if (!manager.LogNeuralNetwork) shouldLogNeuralNetwork = false;
        float[] output = network.FeedForward(input, shouldLogNeuralNetwork);//Call to network to feedforward       
        return output;
    }

    private void RestrictionsForNoteDirectionCheck(Transform saberTip)
    {
        LastSlashedNoteDirection = UpcommingNoteDirection;
        if (UpcommingNoteDirection == Note.NoteDirections.up)
        {
            //Check if the saber is hitting the note top to bottom
            if ((previousSaberTipPos.y + 10) - (saberTip.position.y + 10) > 0 && (previousSaberTipPos.y + 10) > (saberTip.position.y + 10))
            {
                //Check if the saber is hitting the note forwards
                if ((transform.position.z + 10) - (saberTip.position.z + 10) > 0)
                {
                    GiveRewards(manager.PointsAddedForEachNote / 10);
                    hasNoteBeenHitCorrectly = true;
                    lastHitPosition = saberTip.position;
                }
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.down)
        {
            //Check if the saber is hitting the note bottom to top
            if ((saberTip.position.y + 10) - (previousSaberTipPos.y + 10) > 0 && (saberTip.position.y + 10) > (previousSaberTipPos.y + 10))
            {
                //Check if the saber is hitting the note forwards
                if ((transform.position.z + 10) - (saberTip.position.z + 10) > 0)
                {
                    GiveRewards(manager.PointsAddedForEachNote / 10);
                    hasNoteBeenHitCorrectly = true;
                    lastHitPosition = saberTip.position;
                }
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.right)
        {
            //Check if the saber is hitting the note bottom to top
            if ((saberTip.position.x + 10) - (previousSaberTipPos.x + 10) > 0 && (saberTip.position.x + 10) > (previousSaberTipPos.x + 10))
            {
                //Check if the saber is hitting the note forwards
                if ((transform.position.z + 10) - (saberTip.position.z + 10) > 0)
                {
                    GiveRewards(manager.PointsAddedForEachNote / 10);
                    hasNoteBeenHitCorrectly = true;
                    lastHitPosition = saberTip.position;
                }
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.left)
        {
            //Check if the saber is hitting the note bottom to top
            if ((previousSaberTipPos.x + 10) - (saberTip.position.x + 10) > 0 && (previousSaberTipPos.x + 10) > (saberTip.position.x + 10))
            {
                //Check if the saber is hitting the note forwards
                if ((transform.position.z + 10) - (saberTip.position.z + 10) > 0)
                {
                    GiveRewards(manager.PointsAddedForEachNote / 10);
                    hasNoteBeenHitCorrectly = true;
                    lastHitPosition = saberTip.position;
                }
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.up_left)
        {
            //Check if the saber is hitting the note bottom to top
            if ((previousSaberTipPos.x + 10) - (saberTip.position.x + 10) > 0 && (previousSaberTipPos.x + 10) > (saberTip.position.x + 10))
            {
                //Check if the saber is hitting the note forwards
                if ((transform.position.z + 10) - (saberTip.position.z + 10) > 0)
                {
                    GiveRewards(manager.PointsAddedForEachNote / 10);
                    hasNoteBeenHitCorrectly = true;
                    lastHitPosition = saberTip.position;
                }
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.up_right)
        {
            //Check if the saber is hitting the note bottom to top
            if ((saberTip.position.x + 10) - (previousSaberTipPos.x + 10) > 0 && (saberTip.position.x + 10) > (previousSaberTipPos.x + 10))
            {
                //Check if the saber is hitting the note forwards
                if ((transform.position.z + 10) - (saberTip.position.z + 10) > 0)
                {
                    GiveRewards(manager.PointsAddedForEachNote / 10);
                    hasNoteBeenHitCorrectly = true;
                    lastHitPosition = saberTip.position;
                }
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.down_left)
        {
            //Check if the saber is hitting the note bottom to top
            if ((previousSaberTipPos.x + 10) - (saberTip.position.x + 10) > 0 && (previousSaberTipPos.x + 10) > (saberTip.position.x + 10))
            {
                //Check if the saber is hitting the note forwards
                if ((transform.position.z + 10) - (saberTip.position.z + 10) > 0)
                {
                    GiveRewards(manager.PointsAddedForEachNote / 10);
                    hasNoteBeenHitCorrectly = true;
                    lastHitPosition = saberTip.position;
                }
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.down_right)
        {
            //Check if the saber is hitting the note bottom to top
            if ((saberTip.position.x + 10) - (previousSaberTipPos.x + 10) > 0 && (saberTip.position.x + 10) > (previousSaberTipPos.x + 10))
            {
                //Check if the saber is hitting the note forwards
                if ((transform.position.z + 10) - (saberTip.position.z + 10) > 0)
                {
                    GiveRewards(manager.PointsAddedForEachNote / 10);
                    hasNoteBeenHitCorrectly = true;
                    lastHitPosition = saberTip.position;
                }
            }
        }
    }

    private void RestrictionForNoteBeforeSwingCheck(float difficulty)
    {
        if (UpcommingNoteDirection == Note.NoteDirections.up)
        {
            //Check if before a hit, the saber reached a certain angle
            var difference = (transform.Find("SaberTip").position.y + 10) - (transform.position.y + 10);
            if (difference > (0.5f / difficulty))
            {
                angleReached = true;
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.down)
        {
            //Check if before a hit, the saber reached a certain angle
            var difference = ((transform.position.y + 10) - (transform.Find("SaberTip").position.y + 10));
            if (difference > (0.5f / difficulty))
            {
                angleReached = true;
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.right)
        {
            //Check if before a hit, the saber reached a certain angle
            var difference = ((transform.position.x + 10) - (transform.Find("SaberTip").position.x + 10));
            if (difference > (0.5f / difficulty))
            {
                angleReached = true;
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.left)
        {
            //Check if before a hit, the saber reached a certain angle
            var difference = ((transform.Find("SaberTip").position.x + 10) - (transform.position.x + 10));
            if (difference > (0.5f / difficulty))
            {
                angleReached = true;
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.up_left)
        {
            //Check if before a hit, the saber reached a certain angle
            var differencey = ((transform.Find("SaberTip").position.y + 10) - (transform.position.y + 10));
            var differencex = ((transform.Find("SaberTip").position.x + 10) - (transform.position.x + 10));
            if (differencey > (0.5f / difficulty) && differencex > (0.5f / difficulty))
            {
                angleReached = true;
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.up_right)
        {
            //Check if before a hit, the saber reached a certain angle
            var differencey = ((transform.Find("SaberTip").position.y + 10) - (transform.position.y + 10));
            var differencex = ((transform.position.x + 10) - (transform.Find("SaberTip").position.x + 10));
            if (differencey > (0.5f / difficulty) && differencex > (0.5f / difficulty))
            {
                angleReached = true;
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.down_left)
        {
            //Check if before a hit, the saber reached a certain angle
            var differencey = ((transform.position.y + 10) - (transform.Find("SaberTip").position.y + 10));
            var differencex = ((transform.Find("SaberTip").position.x + 10) - (transform.position.x + 10));
            if (differencey > (0.5f / difficulty) && differencex > (0.5f / difficulty))
            {
                angleReached = true;
            }
        }

        if (UpcommingNoteDirection == Note.NoteDirections.down_right)
        {
            //Check if before a hit, the saber reached a certain angle
            var differencey = ((transform.position.y + 10) - (transform.Find("SaberTip").position.y + 10));
            var differencex = ((transform.position.x + 10) - (transform.Find("SaberTip").position.x + 10));
            if (differencey > (0.5f / difficulty) && differencex > (0.5f / difficulty))
            {
                angleReached = true;
            }
        }
    }

    private void RestrictionForNoteAfterSwingCheck(float difficulty)
    {
        if (LastSlashedNoteDirection != Note.NoteDirections.none)
        {
            //UP
            if (LastSlashedNoteDirection == Note.NoteDirections.up)
            {
                if ((lastHitPosition.y + 10) - (transform.Find("SaberTip").position.y + 10) > (2f / difficulty))
                {
                    Reset();
                    GiveRewards(manager.PointsAddedForEachNote);
                    energy = manager.SaberEnergy;
                }
            }

            //DOWN
            if (LastSlashedNoteDirection == Note.NoteDirections.down)
            {
                if ((transform.Find("SaberTip").position.y + 10) - (lastHitPosition.y + 10) > (2f / difficulty))
                {
                    Reset();
                    GiveRewards(manager.PointsAddedForEachNote);
                    energy = manager.SaberEnergy;
                }
            }
            //RIGHT
            if (LastSlashedNoteDirection == Note.NoteDirections.right)
            {
                if ((transform.Find("SaberTip").position.x + 10) - (lastHitPosition.x + 10) > (2f / difficulty))
                {
                    Reset();
                    GiveRewards(manager.PointsAddedForEachNote);
                    energy = manager.SaberEnergy;
                }
            }

            //LEFT
            if (LastSlashedNoteDirection == Note.NoteDirections.left)
            {
                if ((lastHitPosition.x + 10) - (transform.Find("SaberTip").position.x + 10) > (2f / difficulty))
                {
                    Reset();
                    GiveRewards(manager.PointsAddedForEachNote);
                    energy = manager.SaberEnergy;
                }
            }

            //UP_LEFT 
            if (LastSlashedNoteDirection == Note.NoteDirections.up_left)
            {
                var differencey = (lastHitPosition.y + 10) - (transform.Find("SaberTip").position.y + 10);
                var differencex = (lastHitPosition.x + 10) - (transform.Find("SaberTip").position.x + 10);
                if (differencey > 1.5f && differencex > (1.5f / difficulty))
                {
                    Reset();
                    GiveRewards(manager.PointsAddedForEachNote);
                    energy = manager.SaberEnergy;
                }
            }

            //UP_RIGHT 
            if (LastSlashedNoteDirection == Note.NoteDirections.up_right)
            {
                var differencey = (lastHitPosition.y + 10) - (transform.Find("SaberTip").position.y + 10);
                var differencex = (transform.Find("SaberTip").position.x + 10) - (lastHitPosition.x + 10);
                if (differencey > 1.5f && differencex > (1.5f / difficulty))
                {
                    Reset();
                    GiveRewards(manager.PointsAddedForEachNote);
                    energy = manager.SaberEnergy;
                }
            }

            //DOWN_LEFT 
            if (LastSlashedNoteDirection == Note.NoteDirections.down_left)
            {
                var differencey = (transform.Find("SaberTip").position.y + 10) - (lastHitPosition.y + 10);
                var differencex = (lastHitPosition.x + 10) - (transform.Find("SaberTip").position.x + 10);
                if (differencey > 1.5f && differencex > (1.5f / difficulty))
                {
                    Reset();
                    GiveRewards(manager.PointsAddedForEachNote);
                    energy = manager.SaberEnergy;
                }
            }

            //DOWN_RIGHT 
            if (LastSlashedNoteDirection == Note.NoteDirections.down_right)
            {
                var differencey = (transform.Find("SaberTip").position.y + 10) - (lastHitPosition.y + 10);
                var differencex = (transform.Find("SaberTip").position.x + 10) - (lastHitPosition.x + 10);
                if (differencey > 1.5f && differencex > (1.5f / difficulty))
                {
                    Reset();
                    GiveRewards(manager.PointsAddedForEachNote);
                    energy = manager.SaberEnergy;
                }
            }
        }
    }

    private void GiveRewards(int points = 0)
    {      
        AddPoints(points);
        if (manager.DebugsEnabled) Debug.Log($"Points added! Total points: {totalPoints}");        
    }

    public void AddPoints(int points)
    {
        totalPoints += points;
    }

    public void UpdateFitness()
    {
        network.fitness = totalPoints * totalPoints;//updates fitness of network for sorting
    }

    private bool IsOutOfEnergy()
    {
        var differencex = Mathf.Abs(transform.rotation.eulerAngles.x - previousRot.eulerAngles.x);
        var differencey = Mathf.Abs(transform.rotation.eulerAngles.y - previousRot.eulerAngles.y);
        if (differencex < 50) energy -= differencex;
        if (differencey < 50) energy -= differencey;
        if (energy < 0) energy = 0;
        if (energy <= 0) return true;
        return false;
    }
}
