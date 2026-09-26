public enum EnemyState
{
    Roaming,       // Patrouille normale (rouge foncé)
    Chasing,       // Joueur détecté / alerté, se déplace vers lui (rouge clair)
    Surveillance,  // Recherche autour de la dernière position connue (jaune)
    BreakingLocker // Casse un casier + animation d'attaque
}
