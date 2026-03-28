using UnityEngine;
using System.Collections.Generic;

public class LessonManager : MonoBehaviour
{
    public AnatomyUIController uiController;
    public List<GameObject> bones; // Drag bones here in order
    private int currentIndex = 0;

    void Start()
    {
        if (bones.Count > 0) ActivateStep(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) NextStep();
    }

    public void NextStep()
    {
        currentIndex++;
        if (currentIndex < bones.Count)
            ActivateStep(currentIndex);
        else
            Debug.Log("Skeleton System Lesson Complete!");
    }

    void ActivateStep(int index)
    {
        // Optional: Add logic here to change bone color/material
        uiController.SetNewTarget(bones[index].transform);
    }
}