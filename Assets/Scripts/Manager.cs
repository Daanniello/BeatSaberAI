using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

public class Manager : MonoBehaviour
{
    //  Websites info
    //https://medium.com/geekculture/introduction-to-neural-network-2f8b8221fbd3
    //https://www.v7labs.com/blog/neural-networks-activation-functions#choose-activation-function


    //Prefabs
    public GameObject NotePrefab;
    private GameObject LeftSaberPrefab;
    public GameObject RightSaberPrefab;
    public GameObject Platform;

    //In-game effects
    [Range(1, 20)] public int NoteCount = 5;
    [Range(0, 8)] public int NoteDirection = 8;
    [Range(50f, 0f)] public float BeforeAngleDifficulty = 1f;
    public bool AfterAngleIsNeeded = true;
    [Range(50f, 0f)] public float AfterAngleDifficulty = 1f;
    private int fixedNoteCount = 0;

    public int PointsAddedForEachNote = 100;
    public float TotalPointsNeeded = 500;
    public int PopulationSize = 50;
    public int SaberEnergy = 300;
    public bool SaberXAsRotationEnabled = true;
    public bool SaberYAsRotationEnabled = true;
    public bool SaberZAsRotationEnabled = true;
    public bool SaberPostitionEnabled;
    public bool RandomNoteForEveryPlatform = true;
    public bool RandomNoteSequence = true;

    [Range(0.0001f, 1f)] public float MutationChance = 0.05f;
    [Range(0f, 1f)] public float MutationStrength = 0.1f;

    //Tools
    public bool ShouldIncreaseNoteCountAutomaticaly;
    public bool ShouldLowerMutationAutomaticaly = true;

    //Efficiency
    [Range(0.1f, 30f)] public float Gamespeed = 10f;

    //OI
    public bool UseSaveFile;
    public bool LogNeuralNetwork = true;
    public bool DebugsEnabled;

    private List<NeuralNetworkDFF> networks;
    public int[] layers = new int[5] { 2, 5, 5, 5, /*12, 16, 16, 12, 12, 8, 8,*/ 3 };//initializing network to the right size
    public NeuralNetworkDFF.activationEnum[] activations = new NeuralNetworkDFF.activationEnum[4] { NeuralNetworkDFF.activationEnum.tanh, NeuralNetworkDFF.activationEnum.tanh, NeuralNetworkDFF.activationEnum.tanh, NeuralNetworkDFF.activationEnum.tanh }; //Giving the actions for the hidden layers and output layer
    private List<Saber> sabers;
    private List<Note> notes;

    private int platformDistance = 40;

    private int generationCount;

    // Start is called before the first frame update
    void Start()
    {
        InitNetworks();
        MutationChance = 0.05f;
        MutationStrength = 0.1f;

    }

    void FixedUpdate()
    {
        ChangeTimeFrameIfNeeded();
    }

    private void ChangeTimeFrameIfNeeded()
    {
        if (fixedNoteCount != NoteCount)
        {
            fixedNoteCount = NoteCount;
            CancelInvoke("CreatePlaySpaces");
            InvokeRepeating("CreatePlaySpaces", 0.1f, NoteCount * 6);

        }
    }

    public void InitNetworks()
    {
        networks = new List<NeuralNetworkDFF>();
        for (int i = 0; i < PopulationSize; i++)
        {
            NeuralNetworkDFF net = new NeuralNetworkDFF(layers, activations);
            if (UseSaveFile)
            {
                net.Load("Assets/Save.txt");//on start load the network save
            }
            else
            {
                net.Load("Assets/Pre-trained.txt");//on start load the network save
            }
            networks.Add(net);
        }
    }

    public void CreatePlaySpaces()
    {
        Time.timeScale = Gamespeed;
        generationCount += 1;
        GameObject.Find("GenerationCounter").GetComponent<TMPro.TextMeshPro>().SetText("Generation: " + generationCount);
        if (sabers != null)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i] != null) GameObject.Destroy(notes[i].gameObject);//if there are Prefabs in the scene this will get rid of them
            }

            var successCount = 0;
            var highestFitness = sabers.Max(x => x.totalPoints);
            var averageFitness = sabers.Average(x => x.totalPoints);
            for (int i = 0; i < sabers.Count; i++)
            {
                if (sabers[i] != null)
                {
                    if (sabers[i].totalPoints >= TotalPointsNeeded) successCount += 1;
                    GameObject.Destroy(sabers[i].gameObject);//if there are Prefabs in the scene this will get rid of them
                }
            }

            GameObject.Find("SuccessRate").GetComponent<TMPro.TextMeshPro>().SetText($"Success Rate: {(successCount * 100) / sabers.Count}%    {successCount}/{sabers.Count}");
            GameObject.Find("Fitness").GetComponent<TMPro.TextMeshPro>().SetText($"Highest Fitness: {highestFitness}");
            GameObject.Find("AvgFitness").GetComponent<TMPro.TextMeshPro>().SetText($"Average Fitness: {averageFitness}");

            //To make learning go faster. Let them change a lot until the first one gets a success
            //Then change the mutation rate to a low number
            //This is important because otherwise the right movements could be dominated by other mutations again and it will take way longer to learn perfection
            //Also check if the goal is at the right amount 
            if (ShouldLowerMutationAutomaticaly)
            {
                if (successCount != 0)
                {
                    MutationChance = 0.5f;
                    MutationStrength = 0.02f;
                }
            }
            if (((successCount * 100) / sabers.Count) > 90 && ShouldIncreaseNoteCountAutomaticaly)
            {
                NoteCount += 1;
                TotalPointsNeeded = NoteCount * 100;
            }

            SortNetworks();//this sorts networks and mutates them
        }
        else
        {
            for (int i = 0; i < PopulationSize; i++)
            {
                Instantiate(Platform, new Vector3(i * platformDistance, -19, 0), transform.rotation * Quaternion.Euler(0f, 0f, 0f));
            }
        }
        sabers = new List<Saber>();
        notes = new List<Note>();
        var randomNote = Random.Range(0, 8);


        for (int i = 0; i < PopulationSize; i++)
        {

            Saber rightSaber = (Instantiate(RightSaberPrefab, new Vector3(i * platformDistance, 1.6f, 0), transform.rotation * Quaternion.Euler(0f, 0f, 0f))).GetComponent<Saber>();
            rightSaber.network = networks[i];//deploys network to each learner
            rightSaber.id = i + 1;



            AddNotes(NoteCount);
            void AddNotes(int noteCount)
            {
                if (RandomNoteForEveryPlatform) randomNote = Random.Range(0, 8);
                for (var x = 1; x <= noteCount; x++)
                {                    
                    Note note = (Instantiate(NotePrefab, new Vector3((i * platformDistance)/* - Random.Range(-3, 3)*/, 1.6f, x * -25), transform.rotation * Quaternion.Euler(0f, 0f, 0f))).GetComponent<Note>();
                    if (NoteDirection != 8)
                    {
                        note.SetNotePositionAndDirection(Note.NotePositions.LowerMiddleLeft, (Note.NoteDirections)NoteDirection);
                    }
                    else
                    {
                        if(RandomNoteSequence) note.SetNotePositionAndDirection(Note.NotePositions.LowerMiddleLeft, (Note.NoteDirections)Random.Range(0, 8));
                        else note.SetNotePositionAndDirection(Note.NotePositions.LowerMiddleLeft, (Note.NoteDirections)randomNote);
                    }
                    notes.Add(note);
                }
            }

            sabers.Add(rightSaber);
        }
    }

    public void SortNetworks()
    {
        for (int i = 0; i < PopulationSize; i++)
        {
            sabers[i].UpdateFitness();//gets bots to set their corrosponding networks fitness
        }
        networks.Sort();
        networks[PopulationSize - 1].Save("Assets/Save.txt");//saves networks weights and biases to file, to preserve network performance
        for (int i = 0; i < PopulationSize / 2; i++)
        {
            networks[i] = networks[i + PopulationSize / 2].copy(new NeuralNetworkDFF(layers, activations));
            networks[i].Mutate((int)(1 / MutationChance), MutationStrength);
        }
    }

    public int[] GetLayers()
    {
        return layers;
    }

    private void CreateBeatSaberMap(int populationID)
    {

        notes.Add((Instantiate(NotePrefab, new Vector3((populationID * platformDistance), 1.6f, 1 * -25), transform.rotation * Quaternion.Euler(0f, 0f, 0f))).GetComponent<Note>().SetNotePositionAndDirection(Note.NotePositions.LowerMiddleLeft, Note.NoteDirections.up));
        notes.Add((Instantiate(NotePrefab, new Vector3((populationID * platformDistance), 1.6f, 2 * -25), transform.rotation * Quaternion.Euler(0f, 0f, 0f))).GetComponent<Note>().SetNotePositionAndDirection(Note.NotePositions.LowerMiddleLeft, Note.NoteDirections.left));
        notes.Add((Instantiate(NotePrefab, new Vector3((populationID * platformDistance), 1.6f, 3 * -25), transform.rotation * Quaternion.Euler(0f, 0f, 0f))).GetComponent<Note>().SetNotePositionAndDirection(Note.NotePositions.LowerMiddleLeft, Note.NoteDirections.up));
        notes.Add((Instantiate(NotePrefab, new Vector3((populationID * platformDistance), 1.6f, 4 * -25), transform.rotation * Quaternion.Euler(0f, 0f, 0f))).GetComponent<Note>().SetNotePositionAndDirection(Note.NotePositions.LowerMiddleLeft, Note.NoteDirections.down));
        notes.Add((Instantiate(NotePrefab, new Vector3((populationID * platformDistance), 1.6f, 5 * -25), transform.rotation * Quaternion.Euler(0f, 0f, 0f))).GetComponent<Note>().SetNotePositionAndDirection(Note.NotePositions.LowerMiddleLeft, Note.NoteDirections.right));
    }
}
