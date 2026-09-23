# windows-spotlight-desktop

Application .NET (WPF + tray) qui recree l'experience Windows Spotlight
**sur le fond d'ecran du bureau**, pendant que vous travaillez :

- Images Windows Spotlight recuperees via l'API non officielle v4
- Titre + description composes directement sur l'image
- La meme image sur tous vos ecrans simultanement (via `SystemParametersInfo`)
- Icone dans la barre systeme avec popup (flyout) : Image suivante / J'aime / Je n'aime pas
- Rotation automatique (par defaut toutes les 4h) + changement manuel a la demande
- "Je n'aime pas" bannit definitivement l'image (liste noire par hash SHA256)
- "J'aime" protege l'image de la suppression automatique et la fait revenir plus souvent

Ce projet remplace, pour l'usage bureau, le projet plus simple
[windows-spotlight-sync](https://github.com/hebus/windows-spotlight-sync) (qui reste
disponible pour alimenter le diaporama natif de Windows).

## Prerequis

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download) (ou le runtime si vous utilisez un
  binaire publie en self-contained)

## Lancer en developpement

```powershell
dotnet run --project src\SpotlightDesktop\SpotlightDesktop.csproj
```

L'application se place dans la barre systeme (aucune fenetre principale). Clic gauche sur
l'icone pour afficher le flyout, clic droit pour le menu complet.

## Publier un executable autonome

```powershell
dotnet publish src\SpotlightDesktop\SpotlightDesktop.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Donnees et configuration

Tout est stocke dans `%LOCALAPPDATA%\SpotlightDesktop\` :

- `images\` — images brutes telechargees (dedupliquees par hash SHA256)
- `wallpaper\current-a.jpg` / `current-b.jpg` — fond d'ecran compose actif (alterne)
- `catalog.json` — catalogue des images (titre, description, like/dislike, historique)
- `settings.json` — configuration (voir ci-dessous)
- `logs\app-*.log` — journal

`settings.json` :

| Cle                       | Defaut  | Description                                  |
|---------------------------|---------|-----------------------------------------------|
| `rotationIntervalMinutes` | `240`   | Intervalle de rotation automatique             |
| `maxImages`                | `50`    | Nombre max d'images conservees                 |
| `batchCount`               | `4`     | Images recuperees par appel API (max 4)        |
| `locale` / `country`       | `fr-FR` / `FR` | Region demandee a l'API                |
| `likeWeightMultiplier`     | `3`     | Poids d'une image "aimee" dans la rotation     |
| `flyoutAutoHideSeconds`    | `6`     | Delai avant fermeture automatique du flyout    |
| `runAtStartup`             | `true`  | (gere via le menu tray, pas ce fichier)        |

Le demarrage automatique avec Windows se gere depuis le menu de l'icone systeme
("Lancer au demarrage de Windows"), via une entree dans
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
