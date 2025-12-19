using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ElementIcons", menuName = "TCG/Element Icon Data")]
public class ElementIconData : ScriptableObject
{
    // 속성과 이미지를 짝지어주는 구조체
    [System.Serializable]
    public struct ElementIcon
    {
        public CardElement element;
        public Sprite icon;
    }

    public List<ElementIcon> elementIcons; // 리스트로 관리

    // 속성을 넣으면 해당 이미지를 찾아주는 함수
    public Sprite GetIcon(CardElement element)
    {
        foreach (var item in elementIcons)
        {
            if (item.element == element)
            {
                return item.icon;
            }
        }
        return null; // 못 찾으면 없음
    }
}