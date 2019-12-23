using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunchTrigger : MonoBehaviour
{
    public Player player;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player.Punch(other.transform);
        }
    }

}
