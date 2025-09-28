using UnityEngine;
using TMPro;

public class ScoreEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelsText;
    [SerializeField] private TextMeshProUGUI stepsText;

    public void Initialize(int rank, SaveData data)
    {
        rankText.text = rank.ToString();
        nameText.text = data.playerName;
        levelsText.text = data.levelsPassed.ToString();
        stepsText.text = data.totalSteps.ToString();
    }
}