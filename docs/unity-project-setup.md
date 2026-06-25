# Unity Project Setup

## Базовая Конфигурация

- Unity Editor: `6000.3.18f1`
- Папка проекта: `UnityProject/`
- Шаблон: Universal 2D / URP 2D
- Целевая платформа MVP: PC
- Модель проекта: premium single-player

## Пакеты MVP

Обязательный минимальный набор:

- `com.unity.inputsystem` - клавиатура, геймпад и будущая запись player input.
- `com.unity.cinemachine` - камера для 2D side-scroller / puzzle room.
- `com.unity.test-framework` - EditMode и PlayMode тесты core-логики.
- `com.unity.render-pipelines.universal` - URP 2D renderer и VFX для rewind/clone readability.
- `com.unity.ugui` - базовый HUD и fail-state UI.
- `com.unity.localization` - локализация HUD, fail-state и tutorial-текстов.
- `com.unity.2d.tilemap` и `com.unity.2d.tilemap.extras` - сборка puzzle-комнат.

## TextMeshPro

TextMeshPro входит в Unity Editor и не требует отдельного package dependency в `manifest.json`.

При первом открытии проекта нужно импортировать TMP Essential Resources:

```text
Window > TextMeshPro > Import TMP Essential Resources
```

Examples & Extras не импортируем в MVP, чтобы не засорять проект.

## Pixel Perfect

Проект использует URP 2D, поэтому standalone-пакет `com.unity.2d.pixel-perfect` не подключается.

Если визуальный стиль будет pixel-art, используем URP 2D Pixel Perfect-интеграцию и при необходимости Cinemachine Pixel Perfect extension. Это решение нужно финализировать после выбора визуального стиля.

## Пакеты, Которые Не Подключаем В MVP

- Visual Scripting - core loop требует точной, тестируемой gameplay-логики.
- Multiplayer / Netcode - игра MVP является single-player.
- Unity Version Control / Collab Proxy - проект ведется через Git.
- Addressables - преждевременно до появления контентной структуры уровней.
- Unity Gaming Services / Analytics - преждевременно до фиксации локального MVP-среза.
- DOTS/ECS - не требуется для текущего масштаба 2D puzzle-room проекта.

## Git

Корневой `.gitignore` настроен так, чтобы не хранить generated Unity folders:

- `UnityProject/Library/`
- `UnityProject/Temp/`
- `UnityProject/Obj/`
- `UnityProject/Logs/`
- `UnityProject/UserSettings/`

В репозитории должны храниться:

- `UnityProject/Assets/`
- `UnityProject/Packages/`
- `UnityProject/ProjectSettings/`
- документация в `docs/`

## Локальная Проверка Time Rewind MVP

После импорта скриптов в Unity:

1. Открыть меню `RetroTimers > Build Time Rewind Test Scene`.
2. Открыть сцену `Assets/Scenes/TimeRewindTestScene.unity`, если Unity не сделала это автоматически.
3. Нажать Play.
4. Управление:
   - `A/D` или стрелки - движение;
   - `Space/W/Up` - прыжок;
   - `R` - ручная отмотка;
   - `Restart` на HUD - рестарт после fail-state.
5. Проверочный сценарий:
   - первой итерацией дойти до желтой нажимной плиты;
   - нажать `R`;
   - убедиться, что новая управляемая итерация физически появляется только после задержки;
   - убедиться, что клон повторяет маршрут и открывает красную дверь;
   - второй итерацией пройти через дверь к зеленому выходу.
6. Проверка приоритета игрока:
   - перепрыгнуть playback-клона или запрыгнуть на него;
   - встать текущим игроком перед playback-клоном;
   - убедиться, что клон останавливается и не проталкивает игрока;
   - сдвинуть клона игроком или освободить путь и проверить, что клон продолжает движение.
7. Проверка debug-режима смещения:
   - в HUD оставить `Clone mode: Recover to recording` и сдвинуть стоящего на плите клона; он должен стремиться вернуться к записанной позиции;
   - нажать `Toggle clone mode`, чтобы включить `Preserve displacement`;
   - снова сдвинуть стоящего на плите клона и отпустить; он должен остаться на новом месте, если в записи в этот момент он стоял.

Текущий MVP-дефолт для тестового prefab: `jumpVelocity = 12`. Это значение выбрано, чтобы высота прыжка была выше роста игрока и позволяла стабильно использовать клонов как физический объект puzzle-сценария.

Если в сцене остался вручную перетащенный `MvpPlayer`, его нужно удалить и заново выполнить `RetroTimers > Build Time Rewind Test Scene`. Контроллер имеет fallback для scene-template игрока, но чистая проверочная сцена должна ссылаться на prefab автоматически.

## WebGL Build И GitHub Pages

Текущий GitHub Pages URL:

```text
https://ilya096.github.io/Retro_Timers/
```

WebGL-сборка для ручной проверки и GitHub Pages выполняется через Editor-команду:

```powershell
& 'C:\_Unity\6000.3.18f1\Editor\Unity.exe' -batchmode -quit -projectPath 'U:\_CODEX\Retro_Timers\UnityProject' -executeMethod RetroTimers.EditorTools.WebGLBuildPipeline.BuildGithubPages -buildOutput 'Builds/WebGL' -logFile -
```

Editor-меню для локального запуска: `RetroTimers > Build WebGL`.

Правила сборки:

- перед WebGL build автоматически пересобирается `Assets/Scenes/TimeRewindTestScene.unity`;
- output по умолчанию: `UnityProject/Builds/WebGL`;
- `Builds/` остается в `.gitignore` и не коммитится в source-ветку;
- source-изменения ведутся в feature-ветке `feature/time-rewind-webgl-pages`;
- опубликованный статический билд хранится в ветке `gh-pages` в корне ветки;
- для GitHub Pages создается `.nojekyll`;
- WebGL compression отключен, чтобы GitHub Pages мог отдавать файлы как обычный статический сайт без дополнительных headers.

Проверенный deploy 2026-06-25:

- source branch: `feature/time-rewind-webgl-pages`
- pages branch: `gh-pages`
- pages commit: `d3842f7`
- Pages source: `gh-pages` / `/`
- HTTP-проверка: `index.html`, `Build/WebGL.loader.js`, `Build/WebGL.wasm` и `Build/WebGL.data` отдают `200`.
