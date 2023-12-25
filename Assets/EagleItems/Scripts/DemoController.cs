using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class DemoController : MonoBehaviour
{

    public Animator animator;
    public float walkspeed = 5;
    public float horizontal;
    public float horizontal_mult = 0.6f;
    public float vertical;
    public float up;
    private float rotationDegreePerSecond = 1000;
    private bool isAttacking = false;
    public bool goingLeft = false;
    public bool goingRight = false;
    public bool goingUp = false;
    public int scoreMultiplier = 1;
    private Vector3 prev_forward;
    public GameObject gamecam;
    public Vector2 camPosition;
    private bool dead;
    public GameObject[] characters;
    public int currentChar = 0;
    public GameObject[] targets;
    public float minAttackDistance;
    public float speedOut = 0.7f;
    public float difThreshold = 0.08f;
    public float htLimit;
    public float baseHt;
    public float htIncrease;
    public Text nameText;
    public Text scoreValue;
    public Text scoreText;
    public GameObject deathText;
    public AudioSource collectSound;
    public MoveNetSinglePoseSample movenetScript;
    public bool detectObject = false;
    public bool isInvincible = false;
    public float sceneChangeTimer;
    public GameObject gameSound;



    void Start()
    {
        //Can be used for initialization purposes
        up = 1f;
        sceneChangeTimer = 5f;
        gameSound = GameObject.FindGameObjectWithTag("sound");
    }

    private void OnTriggerEnter(Collider other)
    {

        //griffin in contact
        if (dead) return;
        detectObject = true;

        if (other.gameObject.CompareTag("Obstacle"))
        {
            //griffin was hit
            // scoreText.enabled = false;
            // scoreValue.enabled = false;
            if(isInvincible) return;
            deathText.SetActive(true);
            dead = true;
            StartCoroutine(selfdestruct());
            Destroy(gameSound);
            sceneChangeTimer = 10f;
        }
        else if (other.gameObject.CompareTag("Collectible"))
        {
            //griffin collected a point
            scoreValue.text = (int.Parse(scoreValue.text) + scoreMultiplier).ToString();
            other.gameObject.SetActive(false);
            collectSound.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {

        //griffin leaves contact

        if (dead) return;
        detectObject = false;

    }

    void FixedUpdate()
    {
        if (animator && !dead)
        {
            //walk
            //horizontal = horizontal_mult * Input.GetAxis("Horizontal");

            float dif = movenetScript.ht_diff;
            if (dif >= difThreshold || dif <= -difThreshold) horizontal = horizontal_mult * dif;

            if (dif >= difThreshold) goingRight = true;
            else if (dif <= -difThreshold) goingLeft = true;
            else
            {
                goingRight = false;
                goingLeft = false;
                horizontal = 0;
            }

            if(movenetScript.leftElbowHt < movenetScript.noseHt && movenetScript.rightElboxHt < movenetScript.noseHt) goingUp = true;
            else goingUp = false;
            up = 1.5f;

            Vector3 stickDirection = new Vector3(horizontal, 0, vertical);
            if(goingUp && transform.position.y < htLimit){
                stickDirection += new Vector3(0, up, 0);
                transform.position = Vector3.MoveTowards(transform.position, transform.position + new Vector3(0, 0.1f, 0.1f), 100f);
            }
            else if(!goingUp && transform.position.y > baseHt){
                up *= -1f;
                stickDirection += new Vector3(0, up, 0);
                transform.position = Vector3.MoveTowards(transform.position, transform.position + new Vector3(0, -0.1f, 0.1f), 100f);
            }

            if (stickDirection.sqrMagnitude > 1) stickDirection.Normalize();
            if (stickDirection != Vector3.zero && !isAttacking)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(stickDirection, Vector3.up), rotationDegreePerSecond * Time.deltaTime);
            GetComponent<Rigidbody>().velocity = transform.forward * speedOut * walkspeed;
        }
    }


    void Update()
    {
        if (!dead)
        {
            // // move camera
            // attack
            if (Input.GetButtonDown("Fire1") || Input.GetButtonDown("Jump") && !isAttacking)
            {
                isAttacking = true;
                animator.SetTrigger("Attack");
                StartCoroutine(stopAttack(1));
                //tryDamageTarget();
            }

            // get Hit
            if (Input.GetKeyDown(KeyCode.N) && !isAttacking)
            {
                isAttacking = true;
                animator.SetTrigger("Hit");
                StartCoroutine(stopAttack(1));
            }

            animator.SetBool("isAttacking", isAttacking);

            // death
            if (Input.GetKeyDown("m"))
                StartCoroutine(selfdestruct());

            //Leave
            if (Input.GetKeyDown("l"))
            {
                if (this.ContainsParam(animator, "Leave"))
                {
                    animator.SetTrigger("Leave");
                    StartCoroutine(stopAttack(1f));
                }
            }
        }
        
        if(dead && sceneChangeTimer >= 0){
            sceneChangeTimer -= Time.deltaTime;
        }

        if(dead && sceneChangeTimer <= 0) SceneManager.LoadScene(0);
        

    }
    GameObject target = null;
    // public void tryDamageTarget()
    // {
    //     target = null;
    //     float targetDistance = minAttackDistance + 1;
    //     foreach (var item in targets)
    //     {
    //         float itemDistance = (item.transform.position - transform.position).magnitude;
    //         if (itemDistance < minAttackDistance)
    //         {
    //             if (target == null) {
    //                 target = item;
    //                 targetDistance = itemDistance;
    //             }
    //             else if (itemDistance < targetDistance)
    //             {
    //                 target = item;
    //                 targetDistance = itemDistance;
    //             }
    //         }
    //     }
    //     if(target != null)
    //     {
    //         transform.LookAt(target.transform);

    //     }
    // }
    public void DealDamage(DealDamageComponent comp)
    {
        if (target != null)
        {
            target.GetComponent<Animator>().SetTrigger("Hit");
            var hitFX = Instantiate<GameObject>(comp.hitFX);
            hitFX.transform.position = target.transform.position + new Vector3(0, target.GetComponentInChildren<SkinnedMeshRenderer>().bounds.center.y, 0);
        }
    }

    public IEnumerator stopAttack(float length)
    {
        yield return new WaitForSeconds(length);
        isAttacking = false;
    }
    public IEnumerator selfdestruct()
    {
        animator.SetTrigger("isDead");
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        dead = true;

        yield return new WaitForSeconds(6f);
    }
    public void setCharacter(int i)
    {
        currentChar += i;

        if (currentChar > characters.Length - 1)
            currentChar = 0;
        if (currentChar < 0)
            currentChar = characters.Length - 1;

        foreach (GameObject child in characters)
        {
            if (child == characters[currentChar])
            {
                child.SetActive(true);
                if (nameText != null)
                    nameText.text = child.name;
            }
            else
            {
                child.SetActive(false);
            }
        }
        animator = GetComponentInChildren<Animator>();
    }

    public bool ContainsParam(Animator _Anim, string _ParamName)
    {
        foreach (AnimatorControllerParameter param in _Anim.parameters)
        {
            if (param.name == _ParamName) return true;
        }
        return false;
    }
}
