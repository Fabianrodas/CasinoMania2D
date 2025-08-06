using UnityEngine;

public class SlotMachineController : MonoBehaviour
{
    public SlotReel[] reels; // arrastra reel1, reel2 y reel3 aquí

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpinAll();
        }
    }

    public void SpinAll()
    {
        for (int i = 0; i < reels.Length; i++)
        {
            reels[i].stopDelay = 2f + i * 0.5f; // efecto escalonado
            reels[i].Spin();
        }
    }
}