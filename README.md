# windows-spotlight-desktop

Application .NET (WPF + tray) qui recree l'experience Windows Spotlight
**sur le fond d'ecran du bureau**, pendant que vous travaillez :

- Images Windows Spotlight recuperees via l'API non officielle v4
- Titre + description composes directement sur l'image
- La meme image sur tous vos ecrans simultanement (via `SystemParametersInfo`)
- Survolez l'angle superieur droit de l'ecran principal : un flyout apparait avec l'image
  active + 3 suggestions cliquables (clic = nouveau fond d'ecran immediat)
- Boutons emoji 👍/👎 sur le flyout, pour l'image active uniquement
- Rotation automatique (par defaut toutes les 4h) + changement manuel a la demande
- "👎 Je n'aime pas" bannit definitivement l'image (liste noire par hash SHA256)
- "👍 J'aime" protege l'image de la suppression automatique et la fait revenir plus souvent

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

⚠️ **Important** : malgre `PublishSingleFile=true`, WPF genere quelques DLL natives a cote
de l'exe (`wpfgfx_cor3.dll`, `PresentationNative_cor3.dll`, `D3DCompiler_47_cor3.dll`,
`PenImc_cor3.dll`, `vcruntime140_cor3.dll`) — elles ne sont **pas** embarquees dans l'exe.
Il faut copier **tout le contenu** du dossier `publish\` (pas seulement le `.exe`) vers son
emplacement final, sinon l'application plante immediatement au lancement (crash natif dans
`KERNELBASE.dll`, sans exception .NET visible puisqu'il se produit avant meme le demarrage
du code applicatif).

Pour une installation propre :

```powershell
$installDir = "$env:LOCALAPPDATA\Programs\SpotlightDesktop"
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
$publishDir = "src\SpotlightDesktop\bin\Release\net9.0-windows10.0.19041.0\win-x64\publish"
Copy-Item "$publishDir\*" -Destination $installDir -Force -Exclude "*.pdb"
```

Puis pointez l'entree de demarrage automatique (menu tray, voir plus bas) ou le raccourci
vers `$installDir\SpotlightDesktop.exe`.

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
