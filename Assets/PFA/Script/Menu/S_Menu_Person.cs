using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PersonaMenuGenerator : MonoBehaviour
{
    [System.Serializable]
    public class ButtonSettings
    {
        public string nom;

        [Header("Position")]
        public float distance = 300f;
        public float angle = 0f;

        [Header("Taille")]
        public Vector2 taille = new Vector2(250f, 100f);

        [Header("Rotation")]
        public float rotation = 0f;

        [Header("Forme")]
        public Sprite sprite;
    }

    [Header("Prefab du bouton")]
    public GameObject buttonPrefab;

    [Header("Nombre de boutons")]
    public int nombreDeBoutons = 5;

    [Header("Centre du menu")]
    public Vector2 centre = Vector2.zero;

    [Header("Alignement")]
    public bool alignerBordsGauches;

    [Header("Effet au survol")]
    [Range(1f, 1.5f)] public float agrandissementAuSurvol = 1.12f;
    [Range(0.5f, 1f)] public float reductionDesAutresBoutons = 0.88f;
    [Min(0.1f)] public float vitesseAnimationSurvol = 12f;

    [Header("Transition au clic")]
    [Range(1f, 2f)] public float agrandissementAuClic = 1.35f;
    [Range(0f, 0.5f)] public float echelleFinaleDesAutres = 0.03f;
    [Min(0.1f)] public float dureeTransitionClic = 0.45f;

    [Header("Paramètres des boutons")]
    public List<ButtonSettings> boutons = new List<ButtonSettings>();

    private int dernierNombreDeBoutons;
    private Vector2 dernierCentre;
    private GameObject dernierButtonPrefab;
    private bool derniereAlignementBordsGauches;
    private readonly List<ButtonSettings> dernieresConfigurations = new List<ButtonSettings>();
    private readonly List<RectTransform> rectsBoutons = new List<RectTransform>();
    private readonly List<CanvasGroup> groupesCanvas = new List<CanvasGroup>();
    private readonly List<Vector3> echellesDepartClic = new List<Vector3>();
    private readonly List<float> transparencesDepartClic = new List<float>();
    private bool configurationInitialisee;
    private int indexBoutonSurvole = -1;
    private int indexBoutonSelectionne = -1;
    private float progressionTransitionClic;

    [ContextMenu("Appliquer le menu principal (4 boutons)")]
    private void AppliquerDispositionMenuPrincipal()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Appliquer le menu principal");
#endif

        nombreDeBoutons = 4;
        alignerBordsGauches = true;
        boutons = new List<ButtonSettings>
        {
            new ButtonSettings
            {
                nom = "JOUER",
                distance = 355.2f,
                angle = 59.5f,
                taille = new Vector2(348f, 98.4f),
                rotation = -10f
            },
            new ButtonSettings
            {
                nom = "OPTIONS",
                distance = 262.8f,
                angle = 46.7f,
                taille = new Vector2(294f, 86.4f),
                rotation = -7f
            },
            new ButtonSettings
            {
                nom = "CRÉDITS",
                distance = 206.4f,
                angle = 29.5f,
                taille = new Vector2(240f, 74.4f),
                rotation = -4f
            },
            new ButtonSettings
            {
                nom = "QUITTER",
                distance = 181.2f,
                angle = 7.6f,
                taille = new Vector2(174f, 57.6f),
                rotation = 0f
            }
        };

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void Start()
    {
        GenererMenu();
    }

    private void Update()
    {
        if (ConfigurationModifiee())
        {
            GenererMenu();
        }

        AnimerBoutons();
    }

    public void GenererMenu()
    {
        if (buttonPrefab == null)
        {
            Debug.LogError("Le prefab du bouton n'est pas assigné.", this);
            MemoriserConfiguration();
            return;
        }

        // Supprime les anciens boutons
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // Crée les boutons
        rectsBoutons.Clear();
        groupesCanvas.Clear();
        echellesDepartClic.Clear();
        transparencesDepartClic.Clear();
        indexBoutonSurvole = -1;
        indexBoutonSelectionne = -1;
        progressionTransitionClic = 0f;
        int nombreDeConfigurations = boutons == null ? 0 : boutons.Count;
        float bordGaucheCommun = 0f;
        if (alignerBordsGauches && nombreDeConfigurations > 0 && boutons[0] != null)
        {
            ButtonSettings premier = boutons[0];
            float anglePremier = premier.angle * Mathf.Deg2Rad;
            float rotationPremiere = premier.rotation * Mathf.Deg2Rad;
            float demiLargeurTournee = 0.5f * (
                Mathf.Abs(Mathf.Cos(rotationPremiere)) * premier.taille.x
                + Mathf.Abs(Mathf.Sin(rotationPremiere)) * premier.taille.y);
            bordGaucheCommun = Mathf.Cos(anglePremier) * premier.distance - demiLargeurTournee;
        }

        for (int i = 0; i < nombreDeBoutons && i < nombreDeConfigurations; i++)
        {
            if (boutons[i] == null)
            {
                continue;
            }

            GameObject nouveauBouton = Instantiate(buttonPrefab, transform);
            
            RectTransform rect = nouveauBouton.GetComponent<RectTransform>();

            ButtonSettings settings = boutons[i];

            // Position
            float angleRad = settings.angle * Mathf.Deg2Rad;

            float x = Mathf.Cos(angleRad) * settings.distance;
            float y = Mathf.Sin(angleRad) * settings.distance;

            if (alignerBordsGauches)
            {
                float rotationRad = settings.rotation * Mathf.Deg2Rad;
                float demiLargeurTournee = 0.5f * (
                    Mathf.Abs(Mathf.Cos(rotationRad)) * settings.taille.x
                    + Mathf.Abs(Mathf.Sin(rotationRad)) * settings.taille.y);
                x = bordGaucheCommun + demiLargeurTournee;
            }

            rect.anchoredPosition = centre + new Vector2(x, y);

            // Taille
            rect.sizeDelta = settings.taille;

            // Rotation
            rect.localRotation = Quaternion.Euler(0, 0, settings.rotation);

            rectsBoutons.Add(rect);
            MenuButtonHoverEffect hoverEffect = nouveauBouton.AddComponent<MenuButtonHoverEffect>();
            int indexBouton = rectsBoutons.Count - 1;
            hoverEffect.Initialiser(this, indexBouton);

            CanvasGroup groupeCanvas = nouveauBouton.GetComponent<CanvasGroup>();
            if (groupeCanvas == null)
            {
                groupeCanvas = nouveauBouton.AddComponent<CanvasGroup>();
            }
            groupesCanvas.Add(groupeCanvas);
            Button composantBouton = nouveauBouton.GetComponent<Button>();
            if (composantBouton != null)
            {
                composantBouton.onClick.AddListener(() => SelectionnerBouton(indexBouton));
            }

            // Nom
            nouveauBouton.name = settings.nom;

            // Sprite
            Image image = nouveauBouton.GetComponent<Image>();

            if (image != null && settings.sprite != null)
            {
                image.sprite = settings.sprite;
            }

            // Texte
            TMP_Text texte = nouveauBouton.GetComponentInChildren<TMP_Text>();

            if (texte != null)
            {
                texte.text = settings.nom;
            }
        }

        MemoriserConfiguration();
    }

    public void DefinirBoutonSurvole(int index)
    {
        indexBoutonSurvole = index;
    }

    public void EffacerBoutonSurvole(int index)
    {
        if (indexBoutonSurvole == index)
        {
            indexBoutonSurvole = -1;
        }
    }

    private void SelectionnerBouton(int index)
    {
        if (indexBoutonSelectionne >= 0)
        {
            return;
        }

        indexBoutonSelectionne = index;
        indexBoutonSurvole = -1;
        progressionTransitionClic = 0f;
        echellesDepartClic.Clear();
        transparencesDepartClic.Clear();

        for (int i = 0; i < rectsBoutons.Count; i++)
        {
            echellesDepartClic.Add(rectsBoutons[i] != null ? rectsBoutons[i].localScale : Vector3.one);
            transparencesDepartClic.Add(groupesCanvas[i] != null ? groupesCanvas[i].alpha : 1f);
            if (groupesCanvas[i] != null)
            {
                groupesCanvas[i].blocksRaycasts = false;
            }
        }
    }

    private void AnimerBoutons()
    {
        if (indexBoutonSelectionne >= 0)
        {
            progressionTransitionClic = Mathf.Min(
                1f,
                progressionTransitionClic + Time.unscaledDeltaTime / dureeTransitionClic);
            float transition = Mathf.SmoothStep(0f, 1f, progressionTransitionClic);

            for (int i = 0; i < rectsBoutons.Count; i++)
            {
                RectTransform rect = rectsBoutons[i];
                if (rect == null)
                {
                    continue;
                }

                bool boutonSelectionne = i == indexBoutonSelectionne;
                float facteurFinal = boutonSelectionne ? agrandissementAuClic : echelleFinaleDesAutres;
                rect.localScale = Vector3.Lerp(
                    echellesDepartClic[i],
                    Vector3.one * facteurFinal,
                    transition);

                CanvasGroup groupeCanvas = groupesCanvas[i];
                if (groupeCanvas != null)
                {
                    float alphaFinal = boutonSelectionne ? transparencesDepartClic[i] : 0f;
                    groupeCanvas.alpha = Mathf.Lerp(transparencesDepartClic[i], alphaFinal, transition);
                }
            }

            return;
        }

        float facteurLissage = 1f - Mathf.Exp(-vitesseAnimationSurvol * Time.unscaledDeltaTime);

        for (int i = 0; i < rectsBoutons.Count; i++)
        {
            RectTransform rect = rectsBoutons[i];
            if (rect == null)
            {
                continue;
            }

            float echelleCible = 1f;
            if (indexBoutonSurvole >= 0)
            {
                echelleCible = i == indexBoutonSurvole
                    ? agrandissementAuSurvol
                    : reductionDesAutresBoutons;
            }

            Vector3 cible = Vector3.one * echelleCible;
            rect.localScale = Vector3.Lerp(rect.localScale, cible, facteurLissage);
        }
    }

    private bool ConfigurationModifiee()
    {
        if (!configurationInitialisee
            || dernierNombreDeBoutons != nombreDeBoutons
            || !dernierCentre.Equals(centre)
            || dernierButtonPrefab != buttonPrefab
            || derniereAlignementBordsGauches != alignerBordsGauches)
        {
            return true;
        }

        int nombreDeConfigurations = boutons == null ? 0 : boutons.Count;
        if (dernieresConfigurations.Count != nombreDeConfigurations)
        {
            return true;
        }

        for (int i = 0; i < nombreDeConfigurations; i++)
        {
            ButtonSettings actuelle = boutons[i];
            ButtonSettings precedente = dernieresConfigurations[i];

            if (actuelle == null || precedente == null)
            {
                if (actuelle != precedente)
                {
                    return true;
                }

                continue;
            }

            if (actuelle.nom != precedente.nom
                || actuelle.distance != precedente.distance
                || actuelle.angle != precedente.angle
                || !actuelle.taille.Equals(precedente.taille)
                || actuelle.rotation != precedente.rotation
                || actuelle.sprite != precedente.sprite)
            {
                return true;
            }
        }

        return false;
    }

    private void MemoriserConfiguration()
    {
        dernierNombreDeBoutons = nombreDeBoutons;
        dernierCentre = centre;
        dernierButtonPrefab = buttonPrefab;
        derniereAlignementBordsGauches = alignerBordsGauches;
        dernieresConfigurations.Clear();

        if (boutons != null)
        {
            foreach (ButtonSettings settings in boutons)
            {
                dernieresConfigurations.Add(settings == null ? null : new ButtonSettings
                {
                    nom = settings.nom,
                    distance = settings.distance,
                    angle = settings.angle,
                    taille = settings.taille,
                    rotation = settings.rotation,
                    sprite = settings.sprite
                });
            }
        }

        configurationInitialisee = true;
    }
}
