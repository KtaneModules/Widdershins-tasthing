using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class widdershins : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable[] arrowButtons;
    public KMSelectable clearButton;
    public KMSelectable submitButton;
    public TextMesh wordText;
    public TextMesh numberText;
    public Transform arrow;

    private int stage;
    private int selectedDirection;
    private int correctDirection;
    private int previousDirection;
    private int offset;
    private bool isCcw;
    private bool inverted;

    private static readonly float[] rotations = new float[8] { 135f, 180f, 225f, 270f, 315f, 0f, 45f, 90f };
    private static readonly string[] cwWords = new string[4] { "dextral", "forwards", "dextrorotatory", "right" };
    private static readonly string[] ccwWords = new string[4] { "sinistral", "backwards", "levorotatory", "left" };
    private static readonly string[][] positiveExtras = new string[2][] { new string[2] { "handed", "true" }, new string[2] { "super", "ultra" } };
    private static readonly string[][] negativeExtras = new string[2][] { new string[2] { "not", "n’t" }, new string[2] { "counter", "contra" } };
    private static readonly string[] directionNames = new string[8] { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };
    private Coroutine arrowMovement;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in arrowButtons)
            button.OnInteract += delegate () { PressArrowButton(button); return false; };
        clearButton.OnInteract += delegate () { PressClearButton(); return false; };
        submitButton.OnInteract += delegate () { PressSubmitButton(); return false; };
    }

    void Start()
    {
        selectedDirection = rnd.Range(0, 8);
        arrow.localEulerAngles = new Vector3(90f, rotations[selectedDirection], 45f);
        Debug.LogFormat("[Widdershins #{0}] You are at {1}.", moduleId, directionNames[selectedDirection]);
        GenerateStage();
    }

    void PressArrowButton(KMSelectable button)
    {
        button.AddInteractionPunch(.75f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        var offsets = new int[] { -1, 1 };
        var ix = Array.IndexOf(arrowButtons, button);
        SetArrow((selectedDirection + 8 + offsets[ix]) % 8);
    }

    void SetArrow(int direction, bool resetting = false)
    {
        if (arrowMovement != null)
            StopCoroutine(arrowMovement);
        if (selectedDirection != direction)
        {
            selectedDirection = direction;
            arrowMovement = StartCoroutine(MoveArrow(selectedDirection, resetting));
        }
    }

    void PressSubmitButton()
    {
        audio.PlaySoundAtTransform("click", submitButton.transform);
        if (moduleSolved)
            return;
        if (selectedDirection != correctDirection)
        {
            module.HandleStrike();
            Debug.LogFormat("[Widdershins #{0}] You submitted {1}. That is incorrect. Strike!", moduleId, directionNames[selectedDirection]);
        }
        else
        {
            Debug.LogFormat("[Widdershins #{0}] You submitted {1}. That is correct.", moduleId, directionNames[selectedDirection]);
            stage++;
            GenerateStage();
        }
    }

    void PressClearButton()
    {
        audio.PlaySoundAtTransform("click", clearButton.transform);
        if (moduleSolved)
            return;
        SetArrow(previousDirection, resetting: true);
    }

    void GenerateStage()
    {
        previousDirection = selectedDirection;
        if (stage == 4)
        {
            module.HandlePass();
            moduleSolved = true;
            Debug.LogFormat("[Widdershins #{0}] Module solved!", moduleId);
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            wordText.text = "";
            numberText.text = "";
        }
        else
        {
            if (stage != 0)
                Debug.LogFormat("[Widdershins #{0}] Progressing to the next stage...", moduleId);
            isCcw = rnd.Range(0, 2) == 0;
            inverted = rnd.Range(0, 2) == 0;
            offset = rnd.Range(1, 8);
            if (isCcw)
                offset *= -1;
            if (inverted)
                offset *= -1;
            correctDirection = selectedDirection + offset;
            if (correctDirection < 0)
                correctDirection = 8 - (correctDirection * -1);
            else
                correctDirection %= 8;
            var message = isCcw ? ccwWords.PickRandom() : cwWords.PickRandom();
            if (inverted)
            {
                var extraIx = rnd.Range(0, 2);
                if (extraIx == 0)
                    message += negativeExtras[extraIx].PickRandom();
                else
                    message = negativeExtras[extraIx].PickRandom() + message;
            }
            else
            {
                if (rnd.Range(0, 2) == 0)
                {
                    var extraIx = rnd.Range(0, 2);
                    if (extraIx == 0)
                        message += positiveExtras[extraIx].PickRandom();
                    else
                        message = positiveExtras[extraIx].PickRandom() + message;
                }
            }
            wordText.text = message;
            numberText.text = Mathf.Abs(offset).ToString();
            var direction = "";
            if ((isCcw && inverted) || (!isCcw && !inverted))
                direction = "clockwise";
            else
                direction = "counterclockwise";
            Debug.LogFormat("[Widdershins #{0}] Stage {1}: The display says {2}.", moduleId, stage + 1, wordText.text);
            Debug.LogFormat("[Widdershins #{0}] This means you need to set the arrow {1} places {2} from the current position. That's {3}.", moduleId, Mathf.Abs(offset), direction, directionNames[correctDirection]);
        }
    }

    IEnumerator MoveArrow(int end, bool resetting)
    {
        audio.PlaySoundAtTransform(resetting ? "reset" : "squeak", arrow);
        var elapsed = 0f;
        var duration = .25f;
        var startRotation = arrow.localRotation;
        var endRotation = Quaternion.Euler(90f, rotations[end], 45f);
        while (elapsed < duration)
        {
            arrow.localRotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        arrow.localRotation = endRotation;
    }

    // Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} [left/right] [1-7]: Presses that arrow that many times. !{0} submit: Submits the current position. !{0} reset: Resets.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        var cmd = input.ToLowerInvariant();
        if (cmd == "submit")
        {
            yield return null;
            submitButton.OnInteract();
            yield break;
        }
        else if (cmd == "reset")
        {
            yield return null;
            clearButton.OnInteract();
            yield break;
        }
        var m = Regex.Match(cmd, @"^(left|right) ([1-7])$");
        if (!m.Success)
            yield break;
        var ix = m.Groups[1].Value == "left" ? 0 : 1;
        var count = int.Parse(m.Groups[2].Value);
        yield return null;
        for (int i = 0; i < count; i++)
        {
            arrowButtons[ix].OnInteract();
            yield return new WaitForSeconds(.2f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = stage; i < 4; i++)
        {
            while (selectedDirection != correctDirection)
            {
                arrowButtons[0].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            yield return new WaitForSeconds(.2f);
            submitButton.OnInteract();
            yield return new WaitForSeconds(.2f);
        }
        yield return true;
    }
}
