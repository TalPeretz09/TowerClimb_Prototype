using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class TechniqueUI
{
    public string techniqueId;
    public string displayName;
    public bool unlockedByDefault;

    [Header("Child References")]
    public Button techniqueButton;
    public GameObject newTag;
    public GameObject lockIcon;
    public TMP_Text techniqueText;
}

public class TechniqueUnlockManager : MonoBehaviour
{
    [Header("Breadcrumb Notifications")]
    public GameObject trainingBtnNewTag;
    public GameObject techniquesBtnNewTag;

    [Header("Techniques Database")]
    public TechniqueUI[] techniques;

    private void Start()
    {
        foreach (var tech in techniques)
        {
            TechniqueUI currentTech = tech;
            currentTech.techniqueButton.onClick.AddListener(() => MarkTechniqueAsViewed(currentTech));
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        bool anyTechniqueIsNew = false;

        foreach (var tech in techniques)
        {
            int defaultState = tech.unlockedByDefault ? 1 : 0;
            int currentState = PlayerPrefs.GetInt("TechState_" + tech.techniqueId, defaultState);

            if (currentState == 0) // LOCKED
            {
                tech.techniqueButton.interactable = false;
                tech.lockIcon.SetActive(true);
                tech.newTag.SetActive(false);
                tech.techniqueText.text = "-";
            }
            else if (currentState == 1) // UNLOCKED & NEW
            {
                tech.techniqueButton.interactable = true;
                tech.lockIcon.SetActive(false);
                tech.newTag.SetActive(true);
                tech.techniqueText.text = tech.displayName;

                anyTechniqueIsNew = true;
            }
            else if (currentState == 2) // UNLOCKED & VIEWED
            {
                tech.techniqueButton.interactable = true;
                tech.lockIcon.SetActive(false);
                tech.newTag.SetActive(false);
                tech.techniqueText.text = tech.displayName;
            }
        }

        if (trainingBtnNewTag != null) trainingBtnNewTag.SetActive(anyTechniqueIsNew);
        if (techniquesBtnNewTag != null) techniquesBtnNewTag.SetActive(anyTechniqueIsNew);
    }

    private void MarkTechniqueAsViewed(TechniqueUI tech)
    {
        int defaultState = tech.unlockedByDefault ? 1 : 0;
        int currentState = PlayerPrefs.GetInt("TechState_" + tech.techniqueId, defaultState);

        if (currentState == 1)
        {
            PlayerPrefs.SetInt("TechState_" + tech.techniqueId, 2);
            PlayerPrefs.Save();
            RefreshUI();
        }
    }

    public static void UnlockTechnique(string specificTechId)
    {
        if (PlayerPrefs.GetInt("TechState_" + specificTechId, 0) == 0)
        {
            PlayerPrefs.SetInt("TechState_" + specificTechId, 1);
            PlayerPrefs.Save();
        }
    }
}