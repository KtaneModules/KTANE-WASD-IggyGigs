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
    private readonly string TwitchHelpMessage = @"Use '!{0} <W/A/S/D>' to press that button; Chain presses without spaces.";
#pragma warning restore 414

    private char[] _directions = new char[] { 'W', 'A', 'S', 'D' };

    private IEnumerator ProcessTwitchCommand(string command) {
        command = command.Trim().ToUpper();

        if (command.Any(letter => !_directions.Contains(letter))) {
            yield return "sendtochaterror Invalid command!";
        }
        yield return null;

        foreach (char letter in command) {
            Buttons[Array.IndexOf(_directions, letter)].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator TwitchHandleForcedSolve() {
        string pathDirections = string.Empty;
        char[] wasd = "WASD".ToCharArray();
        var pathNodes = new Stack<string>();
        var visitedNodes = new List<int>();
        var yChange = new int[] { -1, 0, 1, 0 };
        var xChange = new int[] { 0, -1, 0, 1 };
        int algX = xCoord;
        int algY = yCoord;
        int count = 0;

        yield return null;
        visitedNodes.Add(7 * algY + algX);
        pathNodes.Push(Map[algY, algX]);

        while (algY != goalY || algX != goalX) {
            // Attempt to alleviate potential lagspikes.
            if (count > 100) {
                count = 0;
                yield return new WaitForSeconds(0.1f);
            }

            string currentNode = pathNodes.Peek();
            if (currentNode == string.Empty) {
                // Move back from dead end.
                pathNodes.Pop();
                int index = Array.IndexOf(wasd, pathDirections[0]);
                visitedNodes.Remove(7 * algY + algX);
                algY -= yChange[index];
                algX -= xChange[index];
                pathDirections = pathDirections.Remove(0, 1);
            }
            else {
                // Travel to new cell.
                int finalIndex = Array.IndexOf(wasd, currentNode[0]);
                int finalDistance = Math.Abs(algX + xChange[finalIndex] - goalX) + Math.Abs(algY + yChange[finalIndex] - goalY);
                foreach (char letter in currentNode) {
                    int index = Array.IndexOf(wasd, letter);
                    int distance = Math.Abs(algX + xChange[index] - goalX) + Math.Abs(algY + yChange[index] - goalY);
                    if (distance < finalDistance) {
                        finalIndex = index;
                        finalDistance = distance;
                    }
                }

                algY += yChange[finalIndex];
                algX += xChange[finalIndex];

                pathNodes.Push(pathNodes.Pop().Remove(0, 1));
                if (visitedNodes.Contains(7 * algY + algX)) {
                    algY -= yChange[finalIndex];
                    algX -= xChange[finalIndex];
                }
                else {
                    visitedNodes.Add(7 * algY + algX);
                    pathDirections = pathDirections.Insert(0, wasd[finalIndex].ToString());
                    pathNodes.Push(Map[algY, algX].Replace(wasd[(finalIndex + 2) % 4].ToString(), ""));
                }
            }
        }
        var solution = new string(pathDirections.Reverse().ToArray());
        yield return ProcessTwitchCommand(solution);
    }
}
