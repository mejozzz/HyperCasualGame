using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnControls;
using DG.Tweening;
using TMPro;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    Rigidbody rb;
    GameManager gameManager;
    NavMeshAgent nav;
    Zone zoneScript;
    Animator anim;

    public Transform[] bones;
    private CameraFollow cameraFollow;

    [Space]

    [Header("Public Reference")]
    public ParticleSystem trailFx;
    public GameObject scoreFx;
    public Transform zone;
    public Transform target;
    public GameObject crown;

    public bool keepBonesStraight;
    public bool isInZone;
    public bool active;
    public bool isAi;

    public float fOutSideZoneRange;

    [Space]

    [Header("Bomber Reference")]
    public bool isBomber;
    public bool unstable;
    public GameObject explosionFx;

    [Space]

    [Header("Punch Reference")]
    public bool canPunch = true;
    public bool stunned;
    public float fPunchForce;
    public GameObject punchFx;
    public Transform puncher;

    [Space]

    [Header("Speed Reference")]
    [SerializeField] float fSpeed = 3.5f;
    [SerializeField] float fTurnSpeed = 5f;
    private float _fDefaultSpeed = 0;

    [Space]

    [Header("Score Reference")]
    public Transform scoreCard;
    public TextMeshPro playerName;
    public int iScore = 0;

    private void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        gameManager = FindObjectOfType<GameManager>();
        cameraFollow = Camera.main.transform.GetComponent<CameraFollow>();

        // AI part
        if (isAi)
        {
            nav = GetComponent<NavMeshAgent>();
            zoneScript = zone.GetComponent<Zone>();
        }
        else
        {
            _fDefaultSpeed = fSpeed;
        }

        InvokeRepeating("AddZoneScore", 1, 1);
    }

    private void OnEnable()
    {
        if (active)
        {
            canPunch = true;
            stunned = false;

            ParticleSystem.EmissionModule em = trailFx.emission;
            em.enabled = true;

            anim.SetBool("isRunning", true);

            rb.velocity = Vector3.zero;

            if (isAi)
            {
                nav.enabled = true;
            }
            else
            {
                cameraFollow.enabled = true;
                fSpeed = _fDefaultSpeed;
            }

            if (isBomber)
            {
                unstable = false;
            }

            Vector3 dir = new Vector3(transform.position.x, 0f, transform.position.z) - new Vector3(zone.position.x, 0f, zone.position.z);
            transform.rotation = Quaternion.LookRotation(-dir);
        }
    }

    private void Update()
    {
        if (gameManager.gameStarted && !active)
        {
            active = true;
            anim.SetBool("isRunning", true);

            ParticleSystem.EmissionModule em = trailFx.emission;
            em.enabled = true;

            if (isAi)
            {
                nav.enabled = true;
            }
        }

        if (active)
        {
            if (isAi)
            {
                if (!stunned)
                {
                    //AI targeting logic
                    if (gameManager.playerInZone.Contains(transform))
                    {
                        target = GetClosestEnemyInZone(gameManager.playerInZone);

                        if (target == null)
                        {
                            target = zoneScript.wayPoints[Random.Range(0, zoneScript.wayPoints.Length) * (int)Time.deltaTime];
                        }
                    }
                    else
                    {
                        target = GetClosestEnemy(gameManager.players);

                        if (Vector3.Distance(transform.position, target.position) > fOutSideZoneRange)
                        {
                            target = zone;
                        }
                    }

                    nav.SetDestination(target.position);
                }
            }
            else
            {
                //Player Controller
                if (Input.GetMouseButton(0))
                {
                    Vector3 touchMagnitude = new Vector3(CnInputManager.GetAxis("Horizontal"), CnInputManager.GetAxis("Vertical"), 0);
                    Vector3 touchPos = transform.position + touchMagnitude.normalized;
                    Vector3 touchDir = touchPos - transform.position;
                    float angel = Mathf.Atan2(touchDir.y, touchDir.x) * Mathf.Rad2Deg;
                    angel -= 90f;
                    Quaternion rot = Quaternion.AngleAxis(angel, Vector3.down);

                    transform.rotation = Quaternion.Lerp(transform.rotation, rot, fTurnSpeed * Mathf.Min(Time.deltaTime, 0.04f));
                }
            }

            // Fall the ground
            if (transform.position.y < -1 && !stunned)
            {
                ParticleSystem.EmissionModule em = trailFx.emission;
                em.enabled = false;
                stunned = true;
                fSpeed = 0f;
                StartCoroutine(Death());
            }
        }

    }

    private void FixedUpdate()
    {
        if (active && !isAi)
        {
            rb.MovePosition(transform.position + transform.forward * fSpeed * Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (keepBonesStraight)
        {
            foreach (Transform bone in bones)
            {
                bone.eulerAngles = new Vector3(0, bone.eulerAngles.y, bone.eulerAngles.z);
            }
        }
    }

    private void AddZoneScore()
    {
        if (isInZone)
        {
            if (isAi)
            {
                iScore++;
            }
            else
            {
                iScore++;

                GameObject go = Instantiate(scoreFx, new Vector3(transform.position.x, transform.position.y + 2, transform.position.z), Quaternion.identity);
                TextMeshPro txt = go.GetComponent<TextMeshPro>();
                txt.DOFade(0, .5f).SetDelay(.5f);
                txt.transform.DOMoveY(txt.transform.position.y + 2, 1);
                Destroy(go, 5f);
            }
        }
    }

    private void AddKnockOutScore()
    {
        if (isAi)
        {
            iScore += 4;
        }
        else
        {
            iScore += 4;

            GameObject go = Instantiate(scoreFx, new Vector3(transform.position.x, transform.position.y + 2, transform.position.z), Quaternion.identity);
            TextMeshPro txt = go.GetComponent<TextMeshPro>();
            txt.text = "+4";
            txt.color = Color.yellow;
            txt.DOFade(0, .5f).SetDelay(.5f);
            txt.transform.DOMoveY(txt.transform.position.y + 2, 1);
            go.transform.DOPunchScale(new Vector3(.5f, .5f, .5f), .8f);
            Destroy(go, 5f);
        }
    }

    Transform GetClosestEnemyInZone(List<Transform> enemies)
    {
        Transform bestTarget = null;
        float smallestDistance = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (Transform potentialTarget in enemies)
        {
            if (potentialTarget != transform)
            {
                Vector3 directionToTarget = potentialTarget.position - currentPos;
                float distance = directionToTarget.sqrMagnitude;

                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    bestTarget = potentialTarget;
                }
            }
        }

        return bestTarget;
    }

    Transform GetClosestEnemy(Player[] enemies)
    {
        Transform bestTarget = null;
        float smallestDistance = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (Player potentialTarget in enemies)
        {
            if (potentialTarget.transform != transform)
            {
                Vector3 directionToTarget = potentialTarget.transform.position - currentPos;
                float distance = directionToTarget.sqrMagnitude;

                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    bestTarget = potentialTarget.transform;
                }
            }
        }

        return bestTarget;
    }

    public void Punch(Transform other)
    {
        if (canPunch)
        {
            if (isBomber)
            {
                if (!unstable)
                {
                    unstable = true;

                    Material mat = transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material;

                    Sequence unstableAnim = DOTween.Sequence();
                    unstableAnim.Append(mat.DOColor(Color.red, .25f));
                    unstableAnim.Join(transform.GetChild(0).DOScale(1.5f, .25f));
                    unstableAnim.Append(mat.DOColor(Color.white, .25f));
                    unstableAnim.Join(transform.GetChild(0).DOScale(1f, .25f));
                    unstableAnim.SetLoops(5);
                    unstableAnim.OnComplete(() =>
                    {
                        GameObject t = Instantiate(explosionFx, new Vector3(transform.position.x, transform.position.y + .5f, transform.position.z), Quaternion.identity);
                        Destroy(t, .5f);

                        StartCoroutine(Death());

                        Collider[] cols = Physics.OverlapSphere(transform.position, 5f);

                        foreach (Collider col in cols)
                        {
                            if (col.CompareTag("Player"))
                            {
                                Rigidbody _rb = col.GetComponent<Rigidbody>();
                                _rb.velocity = Vector3.zero;
                                _rb.velocity = (col.transform.position - transform.position).normalized * fPunchForce;

                                Player _player = col.GetComponent<Player>();

                                _player.StartCoroutine(Stun());
                            }
                        }
                    });

                }
            }

            canPunch = false;
            anim.SetBool("isAttacking", true);
            StartCoroutine(ResetPunch());

            Player tempPlayer = other.GetComponent<Player>();
            tempPlayer.puncher = transform;
            tempPlayer.StartCoroutine(tempPlayer.Stun());

            Rigidbody tempRb = other.GetComponent<Rigidbody>();
            tempRb.velocity = transform.forward * fPunchForce;
        }
    }

    public void Won(bool won)
    {
        active = false;

        ParticleSystem.EmissionModule em = trailFx.emission;
        em.enabled = false;

        anim.SetBool("isRunning", false);

        if (isAi)
        {
            nav.enabled = false;
        }
        else
        {
            fSpeed = 0f;
        }

        if (won)
        {
            anim.SetBool("isWon", true);
        }
        else
        {
            anim.SetBool("isLost", true);
        }
    }

    private IEnumerator ResetPunch()
    {
        yield return new WaitForSeconds(.1f);
        keepBonesStraight = false;
        punchFx.SetActive(true);

        yield return new WaitForSeconds(.25f);
        keepBonesStraight = true;
        punchFx.SetActive(false);
        canPunch = true;

        anim.SetBool("isAttacking", false);

    }

    public IEnumerator Stun()
    {
        stunned = true;
        ParticleSystem.EmissionModule em = trailFx.emission;
        em.enabled = false;

        if (isAi)
        {
            canPunch = false;
            nav.enabled = false;
        }
        else
        {
            fSpeed = 0f;
        }

        yield return new WaitForSeconds(.25f);

        if (Vector3.Distance(Vector3.zero, new Vector3(transform.position.x, 0, transform.position.z)) < 12f) // checking if char is fallen from the ground
        {
            rb.velocity = Vector3.zero;
            stunned = false;
            em.enabled = true;

            if (isAi)
            {
                canPunch = true;
                nav.enabled = true;
            }
            else
            {
                fSpeed = _fDefaultSpeed;
            }
        }
        else
        {
            if (puncher)
            {
                puncher.GetComponent<Player>().AddKnockOutScore();
                puncher = null;
            }

            StartCoroutine(Death());
        }
    }

    private IEnumerator Death()
    {
        if (!isAi)
        {
            cameraFollow.enabled = false;
        }

        yield return new WaitForSeconds(.5f);

        gameObject.SetActive(false);
        transform.position = new Vector3(0, -100f, 0);
        gameManager.StartCoroutine(gameManager.Respawn(transform, 3f));
    }
}
