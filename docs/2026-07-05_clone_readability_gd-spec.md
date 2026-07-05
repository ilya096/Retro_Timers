# Clone Readability: Feature Spec

## 0. Паспорт Документа

- Название фичи: Визуальная Читаемость Клонов
- ID / кодовое имя: `clone_readability`
- Проект / версия / сезон: `Retro Timer`, MVP
- Фичеовнер: `TBD`
- Стейкхолдеры: геймдизайн, gameplay engineering, UX, art, QA
- Статус: validated_in_local_slice
- Дата создания: 2026-07-05
- Дата обновления: 2026-07-05
- Связанные документы:
  - `docs/project-concept.md`
  - `docs/feature-index.md`
  - `docs/2026-07-04_clone_recording_and_playback_gd-spec.md`
  - `docs/2026-06-25_clone_collision_and_interaction_gd-spec.md`
  - `docs/2026-07-05_delayed_clone_control_gd-spec.md`
  - `docs/DECISION_LOG.md`
- История изменений:
  - 2026-07-05: создана первая отдельная spec по результатам интервью; зафиксированы alpha по возрасту, altered flash + tint/outline и level override читаемости.
  - 2026-07-05: добавлена runtime-реализация age-alpha, altered flash, persistent tint/outline и serialized readability defaults.
  - 2026-07-05: текущие цвета игрока, клонов, altered feedback, delayed spawn preview и тестовых объектов вынесены в serialized visual config.
  - 2026-07-05: пользователь подтвердил, что текущая версия работает и стабильна.

## 1. Саммари Фичи

- Что это: визуальные правила, которые помогают отличать активного игрока, playback-клонов разных возрастов, altered-клонов и ghost будущего появления.
- Для кого: для игроков, решающих комнаты через несколько собственных временных итераций.
- Какую проблему решает: снижает риск, что игрок потеряет текущую итерацию или перестанет понимать причинную цепочку клонов.
- Ожидаемый эффект: активный игрок всегда читается главным, playback-клоны различаются по возрасту, а altered-состояние не конфликтует с возрастной прозрачностью.

## 2. Бизнес-Контекст

- Почему фича появилась: core loop с несколькими клонами быстро становится визуально шумным.
- Почему сейчас: фича нужна для будущего tutorial/onboarding и для проверки доверия к playback-клонам.
- Какие альтернативы рассматривались:
  - не различать возраст клонов в MVP;
  - различать только цветом без alpha;
  - показывать altered только debug-текстом.
- Почему выбранный подход лучше альтернатив: alpha снижает шум, active player остается главным, а altered получает отдельный слой feedback без замены возрастного признака.

## 3. Цели

- Главная цель: игрок всегда визуально отличает активную итерацию от playback-клонов.
- Вторичные цели:
  - показать порядок клонов по возрасту без чтения HUD;
  - показать altered-состояние после вмешательства;
  - поддержать роль клонов в пазле: движение, удержание кнопки, опасное положение;
  - дать уровням возможность адаптировать читаемость под фон и освещение.
- Что НЕ является целью:
  - финальный art direction клонов;
  - сложные анимационные состояния;
  - текстовые подсказки над каждым клоном;
  - полная accessibility-система.

## 4. Метрики Успеха

- Основная метрика: игрок не путает активного игрока с playback-клонами.
- Дополнительные:
  - несколько клонов различаются, но старые не исчезают полностью;
  - altered-клон понятен даже при минимальной возрастной alpha;
  - ghost будущего появления не воспринимается как уже активный игрок.
- Guardrail-метрики:
  - прозрачность не делает клонов плохо читаемыми на фоне уровня;
  - altered tint/outline не заменяет возрастную alpha;
  - visual feedback не перегружает сцену.

## 5. Позиционирование В Системе Проекта

- Место в core loop: работает во всех комнатах после первой отмотки.
- Этап игрока: early и дальше; особенно важна в onboarding и комнатах с несколькими ролями клонов.
- Какие системы использует:
  - `clone_recording_and_playback`;
  - `clone_collision_and_interaction`;
  - `delayed_clone_control`;
  - визуальные настройки уровня.
- Какие системы может затронуть/сломать:
  - восприятие активного управления;
  - читаемость altered-состояний;
  - видимость клонов на разных фонах;
  - будущую визуальную систему возраста клонов.

## 6. Scope

### In Scope

- Активный игрок всегда самый заметный: alpha `1.0`.
- Playback-клоны получают alpha по возрасту.
- Дефолты:

```text
clone_newest_alpha = 0.80
clone_alpha_step = 0.20
clone_min_alpha = 0.20
```

- Чем старше playback-клон, тем он прозрачнее.
- Alpha-параметры читаются из config и могут переопределяться уровнем, если фон/свет мешают читаемости.
- Текущие цвета игрока, клонов, delayed spawn preview и тестовых объектов вынесены в serialized visual config для быстрых экспериментов без поиска hardcoded значений.
- Altered feedback: короткий flash при вмешательстве, затем слабый tint + outline.
- Altered feedback применяется поверх текущей возрастной alpha и не меняет age-alpha.
- Ghost будущего появления использует заметность примерно как самый новый playback-клон.

### Out Of Scope

- Финальные спрайты/анимации клонов.
- Полная цветовая accessibility-палитра.
- Роль клона через отдельные иконки.
- Текстовые tutorial-подсказки внутри gameplay-сцены.

### Future

- Визуальные presets под темы уровней.
- Ghost-trail исходной записи для altered-клонов.
- Специальные анимации altered-состояния.
- Accessibility-pass для цветовой слепоты и контрастности.

## 7. Use Cases / УФЧ

### 7.1 Несколько Playback-Клонов

- Триггер: игрок несколько раз отмотал время.
- Реакция системы: новые playback-клоны заметнее, старые более прозрачные, активный игрок остается `100%`.
- Результат: игрок видит порядок итераций без чтения HUD.

### 7.2 Физическое Вмешательство

- Триггер: активный игрок блокирует или сдвигает playback-клона.
- Реакция системы: клон получает короткий flash, затем слабый tint + outline поверх своей возрастной alpha.
- Результат: игрок связывает изменившийся путь клона со своим действием.

### 7.3 Сложный Фон Уровня

- Триггер: стандартная прозрачность делает клонов плохо видимыми.
- Реакция системы: уровень может переопределить alpha-параметры читаемости.
- Результат: читаемость сохраняется без изменения глобальных правил игры.

## 8. Сущности

- Сущность: clone age visual state
  - Назначение: хранит видимость playback-клона по возрасту.
  - Поля/параметры: clone index, newest alpha, alpha step, min alpha.
  - Состояния: newest playback, older playback, min alpha.

- Сущность: altered visual state
  - Назначение: показывает, что текущий игрок вмешался в playback-клона.
  - Поля/параметры: flash duration, altered tint, outline strength.
  - Состояния: inactive, flash, persistent altered.

## 9. Логика Работы

- Активный игрок визуально главнее playback-клонов.
- Playback-клон получает возрастную alpha при создании.
- Старение считается относительно порядка записей: чем раньше запись, тем старше клон и тем ниже alpha.
- Altered feedback не пересчитывает возрастную alpha.
- Если уровень переопределяет alpha-параметры, правило порядка сохраняется.

## 10. UI/UX

- Player-facing HUD не объясняет alpha текстом.
- Age readability передается прозрачностью.
- Altered передается коротким flash и постоянным слабым tint + outline.
- Ghost будущего появления использует визуальный язык player silhouette, но не имеет коллизии.

## 11. Параметризация / Конфиги

- Параметр: `clone_newest_alpha`
  - Тип: float
  - Дефолт: `0.80`
  - Диапазон: `0.35`-`0.95`
  - Override: project default + level override

- Параметр: `clone_alpha_step`
  - Тип: float
  - Дефолт: `0.20`
  - Диапазон: `0.05`-`0.30`
  - Override: project default + level override

- Параметр: `clone_min_alpha`
  - Тип: float
  - Дефолт: `0.20`
  - Диапазон: `0.10`-`0.60`
  - Override: project default + level override

- Параметр: `clone_altered_flash_seconds`
  - Тип: float
  - Дефолт: `0.18`
  - Диапазон: `0.05`-`0.40`

## 12. Формулы, Баланс-Переменные И Локализация

### 12.1 Формулы

```text
clone_age_alpha = max(clone_min_alpha, clone_newest_alpha - age_from_newest * clone_alpha_step)
```

Где `age_from_newest = 0` для самого нового playback-клона.

### 12.2 Переменные Баланса

- `clone_newest_alpha`
- `clone_alpha_step`
- `clone_min_alpha`
- `clone_altered_flash_seconds`
- `visual_config.active_player_color`
- `visual_config.first_clone_color`
- `visual_config.old_clone_color`
- `visual_config.delayed_spawn_preview_color`
- `visual_config.delayed_spawn_progress_color`
- `visual_config.pressure_plate_color`
- `visual_config.door_color`
- `visual_config.exit_color`
- `visual_config.hazard_color`

### 12.3 Ключи Локализации

- Player-facing текстов для MVP нет.
- Debug namespace: `feature.clone_readability`
  - `clone_age_alpha_debug`
  - `clone_altered_debug`

## 13. Контекстные Системы

- Затронутая система: `tutorial_onboarding`
  - Что меняется: onboarding может опираться на визуальное различение активного игрока и playback-клонов.
  - Риск интеграции: если alpha слишком низкая на фоне уровня, игрок не поймет роль старого клона.

- Затронутая система: art/VFX
  - Что меняется: MVP вводит временные alpha/tint/outline правила.
  - Риск интеграции: временный визуальный язык может конфликтовать с финальным art direction.

## 14. Аналитика И Трекинг

- Event map:
  - `clone_visual_state_applied`
  - `clone_altered_feedback_started`
- Обязательные параметры:
  - `level_id`
  - `clone_index`
  - `clone_age_alpha`
  - `altered_by_player`

## 15. Edge Cases И Исключения

- Если alpha делает клона нечитаемым на конкретном фоне, уровень может переопределить параметры.
- Если altered-клон имеет минимальную alpha, tint/outline все равно должны быть видимы.
- Если клон скрыт до первого кадра delayed-записи, age visual применяется после появления.

## 16. Риски И Митигации

- Главный риск: прозрачность может сделать клонов плохо читаемыми на фоне уровня.
  - Митигация: level override alpha-параметров и QA на разных фонах.
- UX-риск: altered-сигнал может конфликтовать с возрастной прозрачностью.
  - Митигация: altered применяется отдельным слоем поверх age-alpha.
- Визуальный риск: много сигналов перегружают экран.
  - Митигация: без текстовых player-facing labels; flash короткий, persistent signal слабый.

## 17. Acceptance Criteria / QA

- Активный игрок всегда визуально отличается от playback-клонов.
- Playback-клоны получают alpha по возрасту из config.
- Дефолты MVP: `0.80`, `0.20`, `0.20`.
- Чем старше playback-клон, тем он прозрачнее.
- Несколько playback-клонов различаются прозрачностью по возрасту.
- Altered-клон получает flash и постоянный слабый tint + outline после вмешательства.
- Altered feedback не меняет возрастную alpha.
- Level override alpha-параметров доступен без правки кода.

### 17.1 MVP Implementation Snapshot

- Runtime-реализация добавлена в `TimeRewindController` и `TimeClonePlayback`.
- Реализовано:
  - config-поля `clone_newest_alpha`, `clone_alpha_step`, `clone_min_alpha`;
  - расчет alpha по возрасту через `TimeRewindRules.CalculateCloneAgeAlpha`;
  - active player alpha остается `1.0`;
  - playback-клоны получают alpha по возрасту при создании;
  - altered-клон получает flash, затем слабый tint + outline;
  - altered feedback сохраняет исходную age-alpha;
  - serialized `TimeRewindVisualConfig` содержит текущие цвета игрока, клонов, preview и тестовых объектов;
  - EditMode-тесты для формулы alpha.
- Статус проверки:
  - `dotnet build UnityProject/UnityProject.sln` завершился без ошибок;
  - пользователь подтвердил, что текущая версия работает и стабильна;
  - Unity batchmode в текущей shell-сессии завершался без stdout/log artifact, поэтому будущие автоматические Play Mode проверки остаются отдельной задачей.

## 18. План Релиза

- Формат выката: локальная проверка в `TimeRewindTestScene`.
- Feature flags: не нужны для MVP; параметры доступны через config/scene values.
- Rollback plan: вернуть одинаковую alpha для всех playback-клонов, если возрастная прозрачность мешает проверке core loop.

## 19. Пострелизный Анализ

- Когда оцениваем: после локальной проверки и первого tutorial/onboarding прототипа.
- Как интерпретируем результат:
  - если активный игрок теряется, усилить его визуальный приоритет;
  - если старые клоны теряются, поднять `clone_min_alpha`;
  - если altered непонятен, усилить flash или outline.
