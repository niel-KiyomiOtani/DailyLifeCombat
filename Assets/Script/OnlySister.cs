﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]

public class OnlySister : MonoBehaviour
{
    [SerializeField] Transform cam;
    public float speed;
    [SerializeField] float jumpForce = 300;

    private Animator animator;
    private const string key_isRun = "isRun";
    private const string key_isJump = "isJump";
    //private const string key_isRunR = "isRunR";
    //private const string key_isRunL = "isRunL";
    private const string key_isWait = "isWait";
    private const string key_isDamaged = "isDamaged";

    Rigidbody rb;
    AudioSource aud;
    GameObject theDest;
    Vector3 latestPos;

    public Transform Dest;
    public AudioClip jumpSE;
    public AudioClip holdSE;
    public AudioClip healSE;

    public float distance; //Rayの距離
    public Transform equipPosition;
    GameObject currentItem;

    bool canGrab;
    bool isJumping = false;

    //アイテムを持つまでの時間
    private float holdTime;
    private float count;
    public Text countText;
    bool isCountdownStart;

    //スキルによる回復
    GameObject hpGage;
    float coolTime = 0.0f;
    bool isHeal=false;
    public Image healIcon;
    public Image unHeal;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        this.animator = GetComponent<Animator>();
        theDest = GameObject.Find("Destination");
        latestPos = GetComponent<Transform>().position;

        rb.constraints = RigidbodyConstraints.FreezeRotation;

        this.aud = GetComponent<AudioSource>();

        this.hpGage = GameObject.Find("hpgage");
    }

    // Update is called once per frame
    void Update()
    {

        float x = Input.GetAxisRaw("Horizontal") * Time.deltaTime * speed;
        float z = Input.GetAxisRaw("Vertical") * Time.deltaTime * speed;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
        {
            //waitからrunに遷移する
            this.animator.SetBool(key_isRun, true);
            this.animator.SetBool(key_isWait, false);
        }
        else
        {
            this.animator.SetBool(key_isWait, true);
        }
        if (Input.GetKeyUp(KeyCode.W) || (Input.GetKeyUp(KeyCode.S)))
        {
            //runからwaitに遷移する
            this.animator.SetBool(key_isRun, false);
        }

        if (Input.GetKey(KeyCode.D))
        {
            //this.animator.SetBool(key_isRunR, true);

            Quaternion rot = Quaternion.AngleAxis(2, Vector3.up);
            Quaternion Q = this.transform.rotation;
            this.transform.rotation = Q * rot;
        }
        else
        {
            //this.animator.SetBool(key_isRunR, false);
        }

        if (Input.GetKey(KeyCode.A))
        {
            // this.animator.SetBool(key_isRunL, true);
            Quaternion rot = Quaternion.AngleAxis(-2, Vector3.up);
            Quaternion Q = this.transform.rotation;
            this.transform.rotation = Q * rot;
        }
        else
        {
            //  this.animator.SetBool(key_isRunL, false);
        }

        //ジャンプ処理
        if (Input.GetKeyDown(KeyCode.Space) && isJumping == false)
        {
            this.rb.AddForce(transform.up * jumpForce);
            this.animator.SetBool(key_isJump, true);

            this.aud.PlayOneShot(this.jumpSE);
            isJumping = true;
        }
        else
        {
            this.animator.SetBool(key_isJump, false);
        }

        //前移動のときだけ方向転換させる
        if (z > 0)
        {
            transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.x, cam.eulerAngles.y, transform.rotation.z));
        }

        transform.position += transform.forward * z + transform.right * x;

        GameObject director = GameObject.Find("GameDirector");
        director.GetComponent<GameDirector>().WinLose();

        CheckGrab();

        if (canGrab)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Invoke("PickUp", holdTime);
                this.aud.PlayOneShot(this.holdSE);
                isCountdownStart = true;
                countText.gameObject.SetActive(true);
            }

        }

        if (IsInvoking("PickUp"))
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Space))
            {
                CancelInvoke();
                countText.gameObject.SetActive(false);
                count = Time.deltaTime;
            }

        }

        if (isCountdownStart)
        {
            count -= Time.deltaTime;
            countText.text = count.ToString("f2");
        }

        if (count < 0)
        {
            countText.gameObject.SetActive(false);
            isCountdownStart = false;
        }

        if (Input.GetKeyDown(KeyCode.Q) && !isHeal)
        {
            this.hpGage.GetComponent<Image>().fillAmount += 0.3f;
            isHeal = true;
            healIcon.gameObject.SetActive(false);
            unHeal.gameObject.SetActive(true);
            this.aud.PlayOneShot(this.healSE);//回復音
        }

        if (isHeal)
        {
            coolTime += Time.deltaTime;

            if (coolTime >= 7.0)
            {
                isHeal = false;
                coolTime = 0.0f;

                unHeal.gameObject.SetActive(false);
                healIcon.gameObject.SetActive(true);
            }
        }


    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("item"))
        {
            GameObject director = GameObject.Find("GameDirector");
            director.GetComponent<GameDirector>().WinLose();
            this.animator.SetBool(key_isDamaged, true);
        }
        else
        {
            this.animator.SetBool(key_isDamaged, false);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Stage"))
        {
            isJumping = false;
        }

    }

    private void CheckGrab()
    {
        Ray ray = new Ray(transform.position + new Vector3(0, 0.13f, 0), transform.forward);
        Ray ray2 = new Ray(transform.position + new Vector3(0, 1.3f, 0), transform.forward);

        RaycastHit hit;
        RaycastHit hit2;

        if (Physics.Raycast(ray, out hit, distance))
        {
            if (hit.transform.CompareTag("item") || hit.transform.CompareTag("spItem"))
            {

                currentItem = hit.transform.gameObject;
                canGrab = true;
                holdTime = hit.collider.gameObject.GetComponent<pickUp>().HTime;

                if (!isCountdownStart)
                {
                    count = hit.collider.gameObject.GetComponent<pickUp>().CountTime;
                }
            }
        }
        else
        {
            canGrab = false;

        }

        if (Physics.Raycast(ray2, out hit2, distance))
        {
            if (hit2.transform.CompareTag("item") || hit2.transform.CompareTag("spItem"))
            {

                currentItem = hit2.transform.gameObject;
                canGrab = true;
                holdTime = hit2.collider.gameObject.GetComponent<pickUp>().HTime;

                if (!isCountdownStart)
                {
                    count = hit2.collider.gameObject.GetComponent<pickUp>().CountTime;
                }
            }

        }
        //else
        //{
        //    canGrab = false;

        //}


        //Raycastの可視化
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
        Debug.DrawRay(ray2.origin, ray2.direction * distance, Color.red);
    }

    //実際にアイテムを持つ
    private void PickUp()
    {
        currentItem.transform.position = equipPosition.position;
        currentItem.transform.parent = equipPosition;
        currentItem.transform.localEulerAngles = equipPosition.transform.localEulerAngles;
        currentItem.GetComponent<Rigidbody>().isKinematic = true;

    }
}

