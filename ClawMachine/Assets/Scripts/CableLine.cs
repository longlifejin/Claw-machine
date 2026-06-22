using UnityEngine;

public class CableLine : MonoBehaviour
{
    [SerializeField] Transform anchor;   // 케이블 상단 고정점 (ClawAssembly의 자식)
    [SerializeField] Transform claw;     // 갈고리 (독립 오브젝트)
    [SerializeField] LineRenderer line;  // 이 오브젝트의 LineRenderer

    void Reset()
    {
        // 컴포넌트 붙일 때 같은 오브젝트의 LineRenderer 자동 연결
        line = GetComponent<LineRenderer>();
    }

    void LateUpdate()
    {
        if (anchor == null || claw == null || line == null) return;

        line.positionCount = 2;
        line.SetPosition(0, anchor.position);  // 상단
        line.SetPosition(1, claw.position);    // 하단(갈고리)
        Debug.Log($"anchor:{anchor.position}, claw:{claw.position}");
    }
}