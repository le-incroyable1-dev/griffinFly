using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class powerupController : MonoBehaviour
{
    public DemoController playerScript;
    public Text speedPowerup;
    public Text invinciblePowerup;
    public Text scorePowerup;
    public Text square;
    public Text circle;
    public Text triangle;
    public Text timerVal;
    public float timeLeft = 0;
    public bool speed = false;
    public bool invinc = false;
    public bool score = false;
    public float curSpeed;

    private void Update(){
        
        if(timerVal.enabled && timeLeft > 0){
            timeLeft -= Time.deltaTime;
            timerVal.text = ((int)timeLeft).ToString();

            if(timeLeft <= 0){
                timeLeft = 0;
                timerVal.enabled = false;

                if(speed){
                    playerScript.speedOut = curSpeed;
                    speedPowerup.enabled = false;
                }

                if(invinc){
                    playerScript.isInvincible = false;
                    invinciblePowerup.enabled = false;
                }

                if(score){
                    playerScript.scoreMultiplier = 1;
                    scorePowerup.enabled = false;
                }

                invinc = false;
                speed = false;
                score = false;
            }
        }
    }

    public void activateSpeedPowerup(){

        if(speed || invinc || score) return;

        if(int.Parse(square.text) == 0) return;
        square.text = (int.Parse(square.text) - 1).ToString();

        curSpeed = playerScript.speedOut;
        speedPowerup.enabled = true;
        playerScript.speedOut = curSpeed*1.5f;
        speed = true;
        ResetTimer();

    }

    public void activateInvincibility(){

        if(speed || invinc || score) return;

        if(int.Parse(circle.text) == 0) return;
        circle.text = (int.Parse(circle.text) - 1).ToString();

        playerScript.isInvincible = true;
        invinciblePowerup.enabled = true;
        invinc = true;
        ResetTimer();

        
    }

    public void activateScoreMultiplier(){

        if(speed || invinc || score) return;

        if(int.Parse(triangle.text) == 0) return;
        triangle.text = (int.Parse(triangle.text) - 1).ToString();

        playerScript.scoreMultiplier = 2;
        scorePowerup.enabled = true;
        score = true;
        ResetTimer();

        
    }

    public IEnumerator waitAndDeactivate(){
        WaitForSeconds wfs = new WaitForSeconds(10f);
        yield return wfs;
    }

    public void ResetTimer(){

        timerVal.enabled = true;
        timeLeft = 5;
    }
}
