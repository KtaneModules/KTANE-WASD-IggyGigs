using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class WasdModule : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable[] Buttons;
    public TextMesh[] DisplayTexts;

    int generatedLocationIndex, startingLocationIndex;

    string[] Locations = { "Bank", "Grocery", "School", "Gym", "Home", "Mall", "Cafe", "Park", "Office" };

    string[,] Map = new string[,] { { "S", "D", "AD", "ASD", "AD", "AD", "AS" },
                                     { "WD", "AS", "SD", "WAD", "AD", "AS", "WS" },
                                     { "SD", "WAD", "WA", "SD", "ASD", "WASD", "WAS" },
                                     { "WD", "AS", "SD", "WA", "WS", "WS", "W" },
                                     { "S", "WS", "WD", "ASD", "WA", "WSD", "A" },
                                     { "WSD", "WAD", "ASD", "WA", "S", "WD", "AS" },
                                     { "W", "D", "WAD", "A", "WD", "AD", "WA" } };

    int xCoord, yCoord;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;

        foreach (KMSelectable Button in Buttons)
        {
            Button.OnInteract += delegate () { ButtonPress(Button); return false; };
        }


        //button.OnInteract += delegate () { buttonPress(); return false; };

    }
    void ButtonPress(KMSelectable Button)
    { //called when button is pressed
        Button.AddInteractionPunch();

        if (ModuleSolved)
            return;
        {

        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Button.transform);
        if (Button.name.Equals("wButton"))
        {
            if (Map[yCoord, xCoord].Contains("W") && yCoord <= 6)
                yCoord--;
            else
                GetComponent<KMBombModule>().HandleStrike(); //incurs strike
        }
        else if (Button.name.Equals("aButton"))
        {
            if (Map[yCoord, xCoord].Contains("A") && xCoord <= 6)
                xCoord--;
            else
                GetComponent<KMBombModule>().HandleStrike(); //incurs strike
        }
        else if (Button.name.Equals("sButton"))
        {
            if (Map[yCoord, xCoord].Contains("S") && yCoord <= 6)
                yCoord++;
            else
                GetComponent<KMBombModule>().HandleStrike(); //incurs strike
        }
        else
        {
            if (Map[yCoord, xCoord].Contains("D") && xCoord<=6)
                xCoord++;
            else
                GetComponent<KMBombModule>().HandleStrike(); //incurs strike
        }
        // COMMENT THIS OUT LATER -- this was used to display values during testing bc i dont know how to use the console :'(
        // DisplayTexts[0].text = "[" + xCoord + "," + yCoord + "] - " + Map[yCoord, xCoord];
        Debug.LogFormat("[WASD #{0}] Available button presses are: {1}. Currently at {2}, {3}.", ModuleId, Map[yCoord, xCoord], yCoord, xCoord);

        if (checkGoal())
        {
            GetComponent<KMBombModule>().HandlePass(); //incurs.. win
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Button.transform);
        }
    }

    void Start()
    { //on generation
        int[] serialNums = Bomb.GetSerialNumberNumbers().ToArray();
        startingLocationIndex = DigitalRoot(serialNums) - 1; //indexed as 0 to 8
        if (startingLocationIndex < 0)
            startingLocationIndex++;

        generatedLocationIndex = Rnd.Range(0, Locations.Length);
        if (generatedLocationIndex == startingLocationIndex)
        {
            int seed = Rnd.Range(-3, 4);
            generatedLocationIndex += seed;
        }
        DisplayTexts[0].text = Locations[generatedLocationIndex]; //random location

        xCoord = (startingLocationIndex % 3) * 3;
        yCoord = (startingLocationIndex / 3) * 3;

        Debug.LogFormat("[WASD #{0}] The displayed location is {1} and the starting location is {2}.", ModuleId, DisplayTexts[0].text, startingLocationIndex + 1);
    }
    void Calculation()
    {

    }

    bool checkGoal()
    {
        // this is why you need 8 hours of sleep
        /*
        int currentPos = (xCoord / 3) + yCoord;
        if (currentPos == generatedLocationIndex)
            return true;
        return false;
        */

        if (yCoord % 3 != 0 || xCoord % 3 != 0)
            return false;
        int goalX, goalY;
        goalX = (generatedLocationIndex % 3) * 3;
        goalY = (generatedLocationIndex / 3) * 3;
        if (xCoord == goalX && yCoord == goalY)
            return true;
        return false;
    }
    int DigitalRoot(int[] numArr)
    {
        int total = 0;
        for (int i = 0; i < numArr.Length; i++)
            total += numArr[i];
        while (total >= 10)
        {
            total = (total % 10) + (total / 10);
        }
        return total;

    }

    void Update()
    {

    }
}