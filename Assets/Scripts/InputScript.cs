﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InputScript : MonoBehaviour
{
    public static InputScript iScript; 
    //NOTE ALL ARRAYS USE 0 AS LEFT AND 1 AS RIGHT.

    //The controllers, used to check if buttons are pressed and location.
    SixenseHand[] m_hands;

    //The Transforms where grabbing occurs
    Transform[] grabPoints;

    //If an object is being dragged it will be stored here so it's parent can be set to null after done.
    Transform[] grabbedObjects;

    //Store the positions of the hands last frame for the velocity when an object is being held.
    Vector3[] lastPositions;

    //How far back the grabbed object appears to look natural.
    public float grabbedObjectOffset = -.05f;

    //If disabled, the player cannot manually shoot.
    public bool shootEnabled = false;

    //If disable, the player cannot manually move.
    public bool moveEnabled = true;

    //If disabled, the AI cannot shoot.
    public bool aiShootEnabled = true;

    //If disable, the player cannot manually move.
    public bool aiMoveEnabled = false;

    //If active, Anna will attempt to mind control ships.
    public bool controlActive = false;

    public float mindControlDelay = 5f;
    public float timeToControl = 5f;

    //The max number of targets in the array. If another is targetted, it will pop one from targetList.
    //Note that priorities are from 0 onward; Anna will first target ships at index 0, then 1, onwards.
    public int maxActiveTargets = 3;
    List<GameObject> targetList = new List<GameObject>();

    //The number of ships Anna can simultaneously attack.
    public int concurrentTargets = 1;

    //An array of the cheat sheet and info screens.
    //public GameObject[] infoScreens;

    public Object laserObject;
    public Object plasmaObject;

    public float laserSpeed = 35;
    public float laserDamage =34;

    public float aiShootDelay = 1;

    private bool currentlyShooting = false;

    //First human upgrade.
    public bool continuousLasers = false;

    //SHIELDS
    public GameObject leftShield;
    public GameObject rightShield;

    // Use this for initialization
    void Start()
    {
        iScript = this;

        m_hands = GetComponentsInChildren<SixenseHand>();
        grabPoints = new Transform[2];
        //grabbedObjects = new Transform[2];
        lastPositions = new Vector3[2];

        for (int i = 0; i < m_hands.Length; i++)
        {
            grabPoints[i] = m_hands[i].transform.Find("GrabPoint").transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < m_hands.Length; i++)
        {
            RaycastHit hit;
            Physics.Raycast(grabPoints[i].position, grabPoints[i].forward, out hit, 100);


            //If it got something, change color of lines and be ready for selecting it.
            if (hit.transform != null)
            {
                grabPoints[i].GetComponent<LineRenderer>().SetColors(Color.red, Color.red);
            }

            else
            {
                grabPoints[i].GetComponent<LineRenderer>().SetColors(Color.white, Color.white);
            }

            //Handle Player Movement
          /*  if (moveEnabled && IsControllerActive(m_hands[i].m_controller) && (Mathf.Abs(SixenseInput.Controllers[i].JoystickX) > .05 || Mathf.Abs(SixenseInput.Controllers[i].JoystickY) > .05))
            {
                //Stores the vector for the force to be added to the ship.
                //Add the forward and backward movements.
                Vector3 movementVector = grabPoints[i].transform.forward * SixenseInput.Controllers[i].JoystickY;

                //Add the left and right movements.
                movementVector += grabPoints[i].transform.up * SixenseInput.Controllers[i].JoystickX;

                movementVector *= Time.deltaTime * 5;
                transform.parent.transform.gameObject.GetComponent<Rigidbody>().AddForce(movementVector, ForceMode.Impulse);

            }*/

            //Handle AI Shooting
            if (aiShootEnabled && IsControllerActive(m_hands[i].m_controller) && m_hands[i].m_controller.GetButtonDown(SixenseButtons.TRIGGER))
            {
                Physics.Raycast(grabPoints[i].position, grabPoints[i].forward, out hit, 100);
                //Debug.Log(hit.transform.gameObject.name);


                //If it got something, and it has health, set it as a target.
                if (hit.transform != null && hit.transform.gameObject.GetComponent<Data>() != null)
                {
                    //If we're at the list, shift everything over 1 and add the new one to the front.
                    if (targetList.Count >= maxActiveTargets)
                    {
                        //First deactivate target on the last element.
                        if (targetList[targetList.Count - 1].transform.Find("Targetted"))
                        {
                            targetList[targetList.Count - 1].transform.Find("Targetted").gameObject.SetActive(false);
                        }
                        //Then shift everything over, so that the last element is overwritten if there is one.
                        for (int index = targetList.Count - 1; index > 0; index--)
                        {
                            targetList[index] = targetList[index - 1];
                        }
                        targetList[0] = hit.transform.gameObject;
                        if (targetList[targetList.Count - 1].transform.Find("Targetted"))
                        {
                            targetList[targetList.Count - 1].transform.Find("Targetted").gameObject.SetActive(true);
                        }
                    }

                    else
                    {
                        //Then shift everything over, so that the last element is overwritten if there is one.
                        for (int index = targetList.Count - 1; index > 0; index--)
                        {
                            targetList[index] = targetList[index - 1];
                        }
                        targetList.Insert(0, hit.transform.gameObject);

                        if (targetList[0].transform.Find("Targetted"))
                        {
                            targetList[0].transform.Find("Targetted").gameObject.SetActive(true);
                        }
                    }

                    //StartCoroutine(bringToPlayer(hit.transform, grabPoints[i], i));
                    //hit.rigidbody.AddForce((m_hands[i].gameObject.transform.position - hit.transform.position) * .5f, ForceMode.Impulse);

                }
            }

            //Handle Human Shooting
            if (shootEnabled && IsControllerActive(m_hands[i].m_controller) && m_hands[i].m_controller.GetButtonDown(SixenseButtons.BUMPER))
            {
                Vector3 shootPosition = grabPoints[i].transform.position;
                //shootPosition.z += 1;

                GameObject lasers = (GameObject)GameObject.Instantiate(laserObject, shootPosition, grabPoints[i].transform.rotation);
                lasers.GetComponent<Laser>().Initialize(false, laserSpeed, 0, laserDamage, grabPoints[i].transform.position + grabPoints[i].transform.forward * 50);

                if (continuousLasers)
                {
                    StartCoroutine(autoFire(i));
                }
            }
            //lastPositions[i] = m_hands[i].transform.position;

        }

        if (aiShootEnabled)
        {
            if (!currentlyShooting && targetList.Count != 0)
            {
                currentlyShooting = true;
                StartCoroutine(annaFire());
            }
        }

    }

    //void aiShoot()
    //{
    //    for (int i = 0; i < targetList.Count; i++)
    //    {
    //        //If some ships are destroyed, then shift the rest of the array left.
    //        while (!targetList[i])
    //        {
    //            targetList.RemoveAt(i);
    //            //Maybe need to handle i going off edge?
    //        }

    //        //If actively being shot, set target to red.
    //        if (i < concurrentTargets)
    //        {
    //            targetList[i].transform.Find("Targetted").gameObject.GetComponent<SpriteRenderer>().color = Color.red;
    //        }

    //        //Else set to white.
    //        else
    //        {
    //            targetList[i].transform.Find("Targetted").gameObject.GetComponent<SpriteRenderer>().color = Color.white;
    //        }

    //    }
    //}


    /** returns true if a controller is enabled and not docked */
    bool IsControllerActive(SixenseInput.Controller controller)
    {
        return (controller != null && controller.Enabled && !controller.Docked);
    }

    IEnumerator annaFire()
    {

        while (targetList.Count != 0)
        {
            timeToControl -= aiShootDelay;
            yield return new WaitForSeconds(aiShootDelay);

            for (int i = 0; i < targetList.Count; i++)
            {
                //If some ships are destroyed, then shift the rest of the array left.
                while (i < targetList.Count && !targetList[i])
                {
                    targetList.RemoveAt(i);
                    yield return null;
                }

                //                Debug.Log(timeToControl);
                //First prioritize mind control.
                /* if (controlActive && timeToControl < 0 && i < targetList.Count)
                 {
                     Debug.Log("ALL YOUR BRAN IS BELONG TO US");
                     timeToControl = mindControlDelay;
                     targetList[i].transform.Find("Targetted").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                     targetList[i].AddComponent<ControlScript>();
                     targetList[i].GetComponent<ControlScript>().Initialize(targetList);
                     targetList.RemoveAt(i);

                 }*/

                //If actively being shot, set target to red.
                if (i < targetList.Count && i < concurrentTargets)
                {
                    if (targetList[i].transform.Find("Targetted"))
                    {
                        targetList[i].transform.Find("Targetted").gameObject.GetComponent<SpriteRenderer>().color = Color.red;
                        GameObject lasers = (GameObject)GameObject.Instantiate(plasmaObject, this.transform.position, this.transform.rotation);
                        lasers.GetComponent<Laser>().Initialize(false, laserSpeed, 0, 100, targetList[i].transform.position);
                    }
                }

                //Else set to white.
                else if (i < targetList.Count)
                {
                    if (targetList[i].transform.Find("Targetted"))
                    {
                        targetList[i].transform.Find("Targetted").gameObject.GetComponent<SpriteRenderer>().color = Color.white;
                    }
                }
                yield return null;
            }



            /*for (int i = 0; i < targetList.Count && i < concurrentTargets; i++)
            {
                //If some ships are destroyed, then shift the rest of the array left.
                while (!targetList[i])
                {
                    targetList.RemoveAt(i);
                    //Maybe need to handle i going off edge?
                }

                GameObject lasers = (GameObject)GameObject.Instantiate(laserObject, this.transform.position, this.transform.rotation);
                lasers.GetComponent<Laser>().Initialize(false, laserSpeed, 0, 1, targetList[i].transform.Find("AI Target").transform.position);
            }*/

            yield return null;

        }
        currentlyShooting = false;
    }

    IEnumerator autoFire(int handNum)
    {
        while (m_hands[handNum].m_controller.GetButton(SixenseButtons.BUMPER))
        {
            yield return new WaitForSeconds(.1f);

            GameObject lasers = (GameObject)GameObject.Instantiate(laserObject, grabPoints[handNum].position, grabPoints[handNum].transform.rotation);
            lasers.GetComponent<Laser>().Initialize(false, laserSpeed, 0, laserDamage, grabPoints[handNum].transform.position + grabPoints[handNum].transform.forward * 50);

            yield return null;
        }
    }
}