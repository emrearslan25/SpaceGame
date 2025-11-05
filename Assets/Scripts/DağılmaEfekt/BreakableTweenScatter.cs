using UnityEngine;
using DG.Tweening;

/// <summary>
/// DOTween tabanlý parçalanma/daðýlma bileþeni.
/// Hoop (Tag: "Hoop") ile temas edince, piecesRoot altýndaki parçalarý
/// belirlenen aralýkta etrafa zýplatarak (DOJump), döndürerek (DORotate),
/// ardýndan fade + scale ile yok eder.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BreakableTweenScatter : MonoBehaviour
{
    [Header("Tetikleme")]
    [SerializeField] private string triggerTag = "Hoop";
    [Tooltip("Trigger kullan. Kapatýrsan normal çarpýþma ile tetiklenir.")]
    [SerializeField] private bool useTrigger = true;
    [Tooltip("Trigger olayý için gerekiyorsa kinematic Rigidbody’yi otomatik ekle.")]
    [SerializeField] private bool autoAddKinematicRigidbody = true;

    [Header("Parçalar")]
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private bool includeInactiveChildren = true;

    [Header("Saçýlma Mesafesi")]
    [SerializeField] private float minScatterDistance = 0.6f;
    [SerializeField] private float maxScatterDistance = 1.6f;

    [Header("Yön Bias'ý")]
    [Range(0f, 1f)][SerializeField] private float hitOutwardBias = 0.7f;
    [Range(0f, 1f)][SerializeField] private float upwardBias = 0.25f;

    [Header("Zýplama/Yol")]
    [SerializeField] private float jumpPower = 0.9f;
    [SerializeField] private float jumpDuration = 0.8f;
    [SerializeField] private int jumpCount = 1;
    [SerializeField] private Ease jumpEase = Ease.OutCubic;

    [Header("Spin / Görsel Sonlandýrma")]
    [SerializeField] private Vector2 randomSpinDegPerAxis = new Vector2(360f, 720f);
    [SerializeField] private float spinDuration = 0.6f;
    [SerializeField] private Ease spinEase = Ease.OutCubic;
    [SerializeField] private float scaleDownDuration = 0.45f;
    [SerializeField] private Ease scaleEase = Ease.InOutQuad;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private Ease fadeEase = Ease.InCubic;
    [SerializeField] private float pieceLifetime = 1.5f;

    [Header("Zamanlama Jitter'ý")]
    [SerializeField] private float perPieceDelayJitter = 0.12f;

    [Header("Opsiyonel")]
    [SerializeField] private GameObject explodeVfx;
    [SerializeField] private bool hideRootRendererOnExplode = true;

    private bool exploded;
    private Vector3 lastHitPoint;

    void Reset()
    {
        piecesRoot = transform;
    }

    void Awake()
    {
        if (piecesRoot == null) piecesRoot = transform;

        var col = GetComponent<Collider>();
        if (!col) Debug.LogWarning("[BreakableTweenScatter] Collider yok. Eklendi mi?");
        col.isTrigger = useTrigger;

        // Trigger olaylarý için en az bir Rigidbody gereksinimi:
        if (useTrigger && autoAddKinematicRigidbody)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }

    // TRIGGER
    void OnTriggerEnter(Collider other)
    {
        if (!useTrigger || exploded) return;
        if (!other.CompareTag(triggerTag)) return;

        lastHitPoint = other.ClosestPoint(transform.position);
        ExplodeInternal();
    }

    // COLLISION
    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger || exploded) return;
        if (!collision.collider.CompareTag(triggerTag)) return;

        lastHitPoint = collision.GetContact(0).point;
        ExplodeInternal();
    }

    void ExplodeInternal()
    {
        exploded = true;

        if (piecesRoot == null)
        {
            Debug.LogWarning("[BreakableTweenScatter] piecesRoot atanmadý. Transform kullanýlacak.");
            piecesRoot = transform;
        }

        if (hideRootRendererOnExplode)
        {
            var r = GetComponent<Renderer>();
            if (r) r.enabled = false;
        }

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (explodeVfx)
            Instantiate(explodeVfx, transform.position, Quaternion.identity);

        var parts = piecesRoot.GetComponentsInChildren<Transform>(includeInactiveChildren);
        bool anyPiece = false;

        foreach (var t in parts)
        {
            if (t == piecesRoot || t == transform) continue; // root’u atla
            anyPiece = true;

            var piece = t.gameObject;
            t.SetParent(null, true);

            // --- hedef pozisyon (geniþ saçýlma + bias) ---
            Vector3 randDir = Random.onUnitSphere;
            randDir.y = Mathf.Lerp(randDir.y, Mathf.Abs(randDir.y), upwardBias);
            randDir.Normalize();

            if (lastHitPoint != Vector3.zero && hitOutwardBias > 0f)
            {
                Vector3 fromHit = (t.position - lastHitPoint).sqrMagnitude > 0.0001f
                    ? (t.position - lastHitPoint).normalized
                    : (t.position - transform.position).normalized;

                randDir = Vector3.Slerp(randDir, fromHit, hitOutwardBias).normalized;
            }

            float dist = Random.Range(minScatterDistance, maxScatterDistance);
            Vector3 targetPos = t.position + randDir * dist;

            float dly = Random.Range(0f, perPieceDelayJitter);

            // 1) zýplama/yer deðiþtirme
            t.DOJump(targetPos, jumpPower, jumpCount, jumpDuration)
                .SetDelay(dly)
                .SetEase(jumpEase);

            // 2) spin
            Vector3 spin = new Vector3(
                Random.Range(randomSpinDegPerAxis.x, randomSpinDegPerAxis.y),
                Random.Range(randomSpinDegPerAxis.x, randomSpinDegPerAxis.y),
                Random.Range(randomSpinDegPerAxis.x, randomSpinDegPerAxis.y)
            );
            if (Random.value < 0.5f) spin *= -1f;

            t.DORotate(t.eulerAngles + spin, spinDuration, RotateMode.FastBeyond360)
                .SetDelay(dly * 0.5f)
                .SetEase(spinEase);

            // 3) fade + scale (renderer mat’lerinde _BaseColor / _Color)
            FadeOutRenderer(piece, Mathf.Max(0f, jumpDuration - fadeDuration * 0.7f));
            t.DOScale(Vector3.zero, scaleDownDuration)
                .SetDelay(Mathf.Max(0f, jumpDuration - scaleDownDuration * 0.6f))
                .SetEase(scaleEase);

            // 4) sil
            Destroy(piece, pieceLifetime);
        }

        if (!anyPiece)
            Debug.LogWarning("[BreakableTweenScatter] Parça bulunamadý. piecesRoot altýnda child var mý?");

        Destroy(gameObject, 0.1f);
    }

    void FadeOutRenderer(GameObject go, float delay)
    {
        var rend = go.GetComponent<Renderer>();
        if (!rend) return;

        // Tüm materyallerde alfa düþürmeyi dene (_BaseColor öncelikli, yoksa _Color)
        foreach (var mat in rend.materials)
        {
            if (mat == null) continue;

            bool hasBase = mat.HasProperty("_BaseColor");
            bool hasColor = mat.HasProperty("_Color");

            if (hasBase)
            {
                Color c = mat.GetColor("_BaseColor");
                mat.DOColor(new Color(c.r, c.g, c.b, 0f), "_BaseColor", fadeDuration)
                   .SetDelay(delay)
                   .SetEase(fadeEase);
            }
            else if (hasColor)
            {
                Color c = mat.GetColor("_Color");
                mat.DOColor(new Color(c.r, c.g, c.b, 0f), "_Color", fadeDuration)
                   .SetDelay(delay)
                   .SetEase(fadeEase);
            }
            else
            {
                // Shader alfa desteklemiyorsa scale ile kapatýrýz (zaten var)
            }
        }
    }
}
