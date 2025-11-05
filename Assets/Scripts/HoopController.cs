using UnityEngine;
using System.Collections;

public class HoopController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeedY = 2f;
    [SerializeField] private float rotationSensitivity = 0.02f;
    [SerializeField] private float maxRotationAngle = 90f;
    [SerializeField] private Transform platformCenter;
    [SerializeField] private float orbitRadius = 2f;

    [Header("Hızlanma Ayarları")]
    [SerializeField] private float speedIncreaseRate = 0.05f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float targetTimeToMaxSpeed = 150f;
    [SerializeField] private float gameTime = 0f;

    [Header("Kamera Ayarları")]
    [SerializeField] private Transform cameraTransform;

    private Vector3 startPos;
    private float currentAngle = 0f;
    private float startY;
    private float initialMoveSpeedY;
    private bool isDragging = false;
    private Vector3 lastMousePos;

    public float CurrentAngle => currentAngle;

    void Start()
    {
        startPos = transform.position;
        startY = startPos.y;
        initialMoveSpeedY = moveSpeedY;
        gameTime = 0f;

        if (platformCenter == null)
        {
            GameObject platform = GameObject.Find("Platform");
            if (platform != null)
                platformCenter = platform.transform;
        }

        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
        }

        if (platformCenter != null)
        {
            Vector3 direction = transform.position - platformCenter.position;
            orbitRadius = new Vector3(direction.x, 0, direction.z).magnitude;
            currentAngle = 0f;
            float radianAngle = currentAngle * Mathf.Deg2Rad;
            Vector3 newPosition = platformCenter.position + new Vector3(
                Mathf.Sin(radianAngle) * orbitRadius,
                transform.position.y - platformCenter.position.y,
                Mathf.Cos(radianAngle) * orbitRadius
            );
            transform.position = newPosition;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
            return;
        }

        gameTime += Time.deltaTime;
        UpdateSpeed();

        Vector3 upwardMovement = Vector3.up * moveSpeedY * Time.deltaTime;
        transform.position += upwardMovement;

        if (cameraTransform != null)
            cameraTransform.position += upwardMovement;

        float currentY = transform.position.y;

        if (platformCenter == null)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            float rotationDelta = delta.x * rotationSensitivity;
            currentAngle += rotationDelta;
            lastMousePos = Input.mousePosition;
        }
#endif

#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float deltaX = touch.deltaPosition.x;
                float rotationDelta = deltaX * rotationSensitivity;
                currentAngle += rotationDelta;
            }
        }
#endif

        currentAngle = Mathf.Clamp(currentAngle, -maxRotationAngle, maxRotationAngle);

        float radianAngle = currentAngle * Mathf.Deg2Rad;
        Vector3 newPosition = platformCenter.position + new Vector3(
            Mathf.Sin(radianAngle) * orbitRadius,
            currentY - platformCenter.position.y,
            Mathf.Cos(radianAngle) * orbitRadius
        );
        transform.position = newPosition;

        Vector3 lookDirection = (platformCenter.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDirection);

        CheckNearbyObstacles();
    }

    void CheckNearbyObstacles()
    {
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        float detectionRadius = 1f;

        foreach (GameObject obstacle in obstacles)
        {
            if (obstacle != null)
            {
                float distance = Vector3.Distance(transform.position, obstacle.transform.position);
                if (distance < detectionRadius)
                {
                    CheckObstacleCollision(obstacle);
                    break;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        CheckObstacleCollision(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        CheckObstacleCollision(collision.gameObject);
    }

    // 🔹 GÜNCELLENMİŞ VERSİYON
    void CheckObstacleCollision(GameObject obj)
    {
        if (obj == null) return;

        // HoopPlus: +1 puan
        if (obj.CompareTag("HoopPlus"))
        {
            ScoreManager.Instance?.Add(1);
            Debug.Log("HoopPlus çarpıldı! +1 puan");
            Destroy(obj);
            return;
        }

        // Obstacle: skor 0 ise oyun durur, >0 ise 1 azalır
        if (obj.CompareTag("Obstacle"))
        {
            int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;

            if (currentScore <= 0)
            {
                StopGameAndMarkObstacle(obj);
            }
            else
            {
                ScoreManager.Instance.Subtract(1);
                Debug.Log("Obstacle çarpıldı! -1 puan");
                Renderer r = obj.GetComponent<Renderer>();
                if (r != null)
                {
                    Color original = r.material.color;
                    r.material.color = Color.yellow;
                    StartCoroutine(ResetColorNextFrame(r, original));
                }
            }
        }
    }

    void StopGameAndMarkObstacle(GameObject obstacle)
    {
        Debug.Log("ENGELE ÇARPTI ve skor 0! Oyun durdu!");
        Time.timeScale = 0f;

        Renderer renderer = obstacle.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.red;
    }

    IEnumerator ResetColorNextFrame(Renderer r, Color target)
    {
        yield return null;
        if (r != null && r.material != null)
            r.material.color = target;
    }

    void UpdateSpeed()
    {
        float timeProgress = Mathf.Clamp01(gameTime / targetTimeToMaxSpeed);
        float easedProgress = Mathf.SmoothStep(0f, 1f, timeProgress);
        float speedDifference = maxSpeed - initialMoveSpeedY;
        moveSpeedY = initialMoveSpeedY + (speedDifference * easedProgress);
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        gameTime = 0f;
        moveSpeedY = initialMoveSpeedY;
        ScoreManager.Instance?.ResetScore();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
