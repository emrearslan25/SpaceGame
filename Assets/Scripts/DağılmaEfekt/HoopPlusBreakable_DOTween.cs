using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class HoopPlusBreakable_DOTween : MonoBehaviour
{
    [Header("Küçük Küpler")]
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private bool includeInactiveChildren = true;

    [Header("Tween Ayarlarý")]
    [SerializeField] private float scatterDistance = 0.5f;     // ne kadar uzaða saçýlacaklar
    [SerializeField] private float scatterDuration = 0.6f;     // saçýlma süresi
    [SerializeField] private float fadeDuration = 0.5f;        // kaybolma süresi
    [SerializeField] private float scaleDownDuration = 0.5f;   // küçülme süresi
    [SerializeField] private float pieceLifetime = 1.2f;       // toplam ömür

    [Header("Easing")]
    [SerializeField] private Ease scatterEase = Ease.OutCubic;
    [SerializeField] private Ease fadeEase = Ease.InCubic;
    [SerializeField] private Ease scaleEase = Ease.InOutQuad;

    [Header("Görsel")]
    [SerializeField] private GameObject explodeVfx;
    [SerializeField] private bool hideRootRendererOnExplode = true;

    private bool exploded;

    void Awake()
    {
        if (piecesRoot == null) piecesRoot = transform;
        var col = GetComponent<Collider>();
        col.isTrigger = true; // Hoop ile temasta çalýþsýn
    }

    void OnTriggerEnter(Collider other)
    {
        if (exploded) return;
        if (!other.CompareTag("Hoop")) return;

        Explode();
    }

    public void Explode()
    {
        exploded = true;

        // ana görseli gizle
        if (hideRootRendererOnExplode)
        {
            var rend = GetComponent<Renderer>();
            if (rend) rend.enabled = false;
        }

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (explodeVfx)
            Instantiate(explodeVfx, transform.position, Quaternion.identity);

        // child parçalarý al
        var pieces = piecesRoot.GetComponentsInChildren<Transform>(includeInactiveChildren);
        foreach (var t in pieces)
        {
            if (t == piecesRoot || t == transform) continue;
            var piece = t.gameObject;

            piece.transform.SetParent(null, true);

            // Her parçaya küçük bir rastgele hedef belirle
            Vector3 randomOffset = Random.insideUnitSphere * scatterDistance;
            randomOffset.y = Mathf.Abs(randomOffset.y); // daha yukarý aðýrlýklý gitsin

            Vector3 targetPos = t.position + randomOffset;

            // Saçýlma animasyonu
            t.DOMove(targetPos, scatterDuration)
                .SetEase(scatterEase);

            // Küçülme
            t.DOScale(Vector3.zero, scaleDownDuration)
                .SetDelay(scatterDuration * 0.5f)
                .SetEase(scaleEase);

            // Fade out (renderer varsa)
            var renderer = piece.GetComponent<Renderer>();
            if (renderer && renderer.material.HasProperty("_Color"))
            {
                Color startColor = renderer.material.color;
                renderer.material
                    .DOColor(new Color(startColor.r, startColor.g, startColor.b, 0f), "_Color", fadeDuration)
                    .SetDelay(scatterDuration * 0.3f)
                    .SetEase(fadeEase);
            }

            // Süre sonunda yok et
            Destroy(piece, pieceLifetime);
        }

        // Ana objeyi de yok et
        Destroy(gameObject, 0.1f);
    }
}
