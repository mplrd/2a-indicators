# 2Ai Indicators — cTrader port

Portage des indicateurs Pine v6 (`tradingview/`) vers cAlgo (cTrader Automate, C#, .NET 6).

Cible plateforme : **cTrader Desktop 5.7+** / cAlgo .NET 6 / namespace `cAlgo.API`.

## Convention nommage

| Niveau | Valeur |
|---|---|
| AssemblyName / DLL sur disque | `2Ai.Indicators.Core.dll` |
| Namespace C# | `_2Ai.Indicators.Core` (préfixe underscore — C# interdit les identifiers commençant par un chiffre) |
| Indicator titre cAlgo | `2Ai Layout`, `2Ai Levels`, etc. (chaîne libre dans `[Indicator(...)]`) |

## Architecture

Même découpage 3-couches que le projet Pine — l'objectif `max reuse` est central.

```
ctrader/
├── Core/                      # Class Library compilée en 2Ai.Indicators.Core.dll
│   ├── Core.csproj
│   ├── Series.cs              # equiv lib_series  (Couche 0)
│   ├── Zone.cs                # equiv lib_zone    (Couche 0)
│   ├── MovingAverages.cs      # equiv lib_ma      (Couche 1)
│   ├── Bollinger.cs           # equiv lib_bollinger (Couche 1)
│   ├── Macd.cs                # equiv lib_macd    (Couche 1)
│   ├── Rsi.cs                 # equiv lib_rsi     (Couche 1)
│   ├── StochRsi.cs            # equiv lib_stochrsi (Couche 1)
│   ├── Divergence.cs          # equiv lib_divergence (Couche 1)
│   ├── ...                    # autres libs au fil du portage
│   └── Draw.cs                # equiv lib_draw    (Couche 2 — helpers Chart.Draw*)
└── Indicators/                # 1 sous-dossier par indicateur cAlgo
    ├── Layout/
    │   ├── Layout.csproj
    │   └── Layout.cs
    ├── Levels/                # à venir
    ├── MacdZr/                # à venir
    └── ...
```

## Build

Chaque indicateur est un projet cAlgo (`.csproj` séparé). Tous référencent `Core` (par projet ou par DLL).

### Setup local

1. **Trouver `cAlgo.API.dll`** sur ta machine. Chemins possibles selon ton install cTrader :
   - `C:\Program Files\cTrader\cAlgo.API.dll`
   - `C:\Users\<toi>\Documents\cAlgo\API\cAlgo.API.dll`
   - Variable d'env cTrader Automate (vérifier dans cTrader Settings → Automate).

2. **Ajuster les `HintPath`** dans les `.csproj` (`Core.csproj` + chaque `Indicators/<Name>/<Name>.csproj`) si la version installée diffère de celle référencée par défaut.

3. **Build `Core`** d'abord (Class Library) → produit `Core/bin/Release/net6.0/2Ai.Indicators.Core.dll`.

4. **Build chaque indicateur** → produit le DLL d'indicateur que cAlgo charge.

### Déploiement vers cTrader

Les indicateurs cAlgo se placent dans : `C:\Users\<toi>\Documents\cAlgo\Sources\Indicators\` (ou équivalent selon l'install). Le DLL `2Ai.Indicators.Core.dll` doit être trouvable par cAlgo — typiquement copié dans le même dossier que le DLL de l'indicateur, ou référencé via le projet.

## Conventions de portage

| Pine | C# / cAlgo |
|---|---|
| `lib_xxx` | `Core/Xxx.cs` (PascalCase, classe static la plupart du temps) |
| `camelCase` Pine | `PascalCase` (méthodes, classes, propriétés) ; `camelCase` (params, locals) |
| `src[N]` (N bars ago) | `src[index - N]` ou `src.Last(N)` |
| `na` Pine | `double.NaN` |
| `simple int` / `series float` | types C# concrets (`int`, `double`, `DataSeries`) |
| Enum Pine | Enum C# |
| UDT Pine (`type Zone`) | Classe C# (`public class Zone`) |
| `bar_index` Pine | param `int index` passé depuis `Calculate(index)` cAlgo |
| `plot()` Pine | propriété `[Output("Name")] public IndicatorDataSeries Result { get; set; }` |
| `box.new`, `line.new`, `label.new` | `Chart.DrawRectangle(...)`, `Chart.DrawTrendLine(...)`, `Chart.DrawText(...)` |

## Ordre de portage

1. `Layout` (Bollinger + MA + Ichimoku + Supertrend + projections)
2. `Levels`
3. `MacdZr`
4. `ZonesSd`
5. `ZonesCmi`, `ZonesMtf`, `Divergences`

Les libs nécessaires sont portées **avant** chaque indicateur consommateur.
