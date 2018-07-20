using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;

public class DialogueBoxController : MonoBehaviour
{
    private Text dialogueTextArea;

    private Text nameTextArea;

    public UnityEvent DialogueBeginAnimation;

    public UnityEvent DialogueStopAnimation;

    public bool needAniamtion;

    public float characterDisplayDuration;

    /// <summary>
    /// The regex expression to find the name
    /// </summary>
    public string namePattern;

    /// <summary>
    /// The group of the name in the name pattern
    /// </summary>
    public int nameGroup;

    private void Start()
    {
        dialogueTextArea = transform.Find("DialogueText").gameObject.GetComponent<Text>();
        nameTextArea = transform.Find("Name/NameText").gameObject.GetComponent<Text>();
    }

    private string currentName;
    private string currentDialogue;

    private Coroutine animationCoroutine;

    private bool isAnimating = false;

    /// <summary>
    /// The content of the dialogue box needs to be changed
    /// </summary>
    /// <param name="text"></param>
    public void OnDialogueChange(string text)
    {
        Debug.Log(string.Format("<color=green><b>{0}</b></color>", text));

        // Parse dialogue text
        var m = Regex.Match(text, namePattern);
        var dialogueStartIndex = 0;
        if (m.Success)
        {
            currentName = m.Groups[nameGroup].Value;
            dialogueStartIndex = m.Length;
        }
        else
        {
            // no name is found
            currentName = "";
        }

        currentDialogue = text.Substring(dialogueStartIndex).Trim();

        // change display
        nameTextArea.text = currentName;
        if (!needAniamtion)
        {
            dialogueTextArea.text = currentDialogue;
            return;
        }

        // need animantion
        if (isAnimating)
        {
            // The last coroutine is still running, kill it
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(CharacterAnimation());
    }

    /// <summary>
    /// Use coroutine to play animation, display the character one by one
    /// </summary>
    /// <returns></returns>
    private IEnumerator CharacterAnimation()
    {
        DialogueBeginAnimation.Invoke();
        isAnimating = true;
        for (var index = 1; index <= currentDialogue.Length; ++index)
        {
            dialogueTextArea.text = currentDialogue.Substring(0, index);
            yield return new WaitForSeconds(characterDisplayDuration);
        }

        // Animation stop
        DialogueStopAnimation.Invoke();
        isAnimating = false;
    }

    /// <summary>
    /// Stop the current animation
    /// </summary>
    public void StopCharacterAnimation()
    {
        if (!isAnimating)
        {
            return;
        }

        StopCoroutine(animationCoroutine);
        DialogueStopAnimation.Invoke();
        isAnimating = false;
        dialogueTextArea.text = currentDialogue;
    }
}