using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
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
    int goalX, goalY;

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
            generatedLocationIndex += Rnd.Range(1, 9);
            generatedLocationIndex %= 9;
        }
        DisplayTexts[0].text = Locations[generatedLocationIndex]; //random location

        xCoord = (startingLocationIndex % 3) * 3;
        yCoord = (startingLocationIndex / 3) * 3;
        goalX = (generatedLocationIndex % 3) * 3;
        goalY = (generatedLocationIndex / 3) * 3;

        Debug.LogFormat("[WASD #{0}] The displayed location is {1} and the starting location is {2}.", ModuleId, DisplayTexts[0].text, startingLocationIndex + 1);
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

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use '!{0} <W/A/S/D>' to press that button; Chain presses without or without spaces.";
#pragma warning restore 414

    private WaitForSeconds _tpInterval = new WaitForSeconds(.1f);

    private IEnumerator ProcessTwitchCommand(string command) {
        command = command.Trim().ToUpperInvariant();

        var match = Regex.Match(command, @"^[WASD ]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (match.Success) {
            yield return null;
            foreach (char c in command) {
                if (char.IsWhiteSpace(c))
                    continue;
                Buttons["WASD".IndexOf(c)].OnInteract();
                yield return _tpInterval;
            }
        }
        yield return "sendtochaterror That command was invalid!";
    }

    private IEnumerator TwitchHandleForcedSolve() {
        // Breadth-First Search, starting from the goal and working towards the current location.
        var pathGrid = new char[7, 7];
        var directionVectors = new Dictionary<char, Vector2Int> {
            { 'W', new Vector2Int(0, -1) },
            { 'A', Vector2Int.left },
            { 'S', new Vector2Int(0, 1) },
            { 'D', Vector2Int.right }
        };
        var visitedCells = new List<Vector2Int>();
        var currentIteration = new List<Vector2Int>();
        var currentCell = new Vector2Int(xCoord, yCoord);

        pathGrid[goalY, goalX] = 'G';
        visitedCells.Add(new Vector2Int(goalX, goalY));
        currentIteration.Add(new Vector2Int(goalX, goalY));

        while (!visitedCells.Contains(currentCell) || !currentIteration.Any()) {
            var newIteration = new List<Vector2Int>();
            foreach (Vector2Int cell in currentIteration) {
                foreach (char dir in Map[cell.y, cell.x]) {
                    Vector2Int newCell = cell + directionVectors[dir];
                    if (!visitedCells.Contains(newCell)) {
                        newIteration.Add(newCell);
                        pathGrid[newCell.y, newCell.x] = dir;
                        visitedCells.Add(newCell);
                    }
                }
            }
            currentIteration = newIteration;
        }
        if (!visitedCells.Contains(currentCell)) {
            yield return "sendtochat The WASD autosolver was not able to find a path to the goal from the current position. Please report this to ku.ro on Discord.";
            GetComponent<KMBombModule>().HandlePass();
            yield break;
        }

        var solution = string.Empty;
        var swaps = new Dictionary<char, char> {
            { 'W', 'S' },
            { 'A', 'D' },
            { 'S', 'W' },
            { 'D', 'A' }
        };
        char currentValue = pathGrid[currentCell.y, currentCell.x];
        while (currentValue != 'G') {
            char dir = swaps[currentValue];
            solution += dir;
            currentCell += directionVectors[dir];
            currentValue = pathGrid[currentCell.y, currentCell.x];
        }
        yield return ProcessTwitchCommand(solution);
    }
}
