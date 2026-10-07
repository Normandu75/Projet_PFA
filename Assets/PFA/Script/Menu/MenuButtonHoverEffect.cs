using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private PersonaMenuGenerator menu;
    private int indexBouton;

    public void Initialiser(PersonaMenuGenerator menuGenerator, int index)
    {
        menu = menuGenerator;
        indexBouton = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menu != null)
        {
            menu.DefinirBoutonSurvole(indexBouton);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (menu != null)
        {
            menu.EffacerBoutonSurvole(indexBouton);
        }
    }
}
