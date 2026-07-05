# Retro Timer: Feature Index

## Feature List

| feature_id | feature_name | brief | player_value | priority | depends_on | distribution | status | spec_doc |
|---|---|---|---|---|---|---|---|---|
| player_movement | Движение И Прыжок | Базовое платформерное управление персонажем на PC с клавиатурой и геймпадом; прыжок в MVP выше роста игрока, чтобы запрыгивать на клонов. | Дает точный контроль в комнатах, где важны позиция, тайминг и физическая блокировка клонов. | MVP | none | all | validated_in_webgl_slice | docs/2026-06-25_player_movement_gd-spec.md |
| level_timer_and_exit | Таймер И Выход | В MVP-срезе уровень имеет лимит времени, выход, победу при достижении выхода игроком или клоном и fail-state при нехватке активного времени после задержки. | Создает давление времени, ясную цель комнаты и конечный предел полезных итераций. | MVP | player_movement | all | implemented_in_mvp_slice | TBD |
| death_and_paradox | Смерть И Парадокс | В MVP-срезе hazard завершает уровень поражением из-за парадокса при смерти игрока или клона. | Делает каждую прошлую итерацию важной и поддерживает напряжение. | MVP | player_movement | all | implemented_in_mvp_slice | TBD |
| time_rewind | Отмотка Времени | Игрок может вручную вернуть уровень к старту, а при истечении таймера игра принудительно отматывает время, если у нового клона останется минимум 1 секунда активного времени. | Превращает ошибку, тупик или истечение времени в часть решения, пока таймер еще позволяет полезную итерацию. | MVP | level_timer_and_exit, death_and_paradox | all | validated_in_local_slice | docs/2026-06-25_time_rewind_gd-spec.md |
| clone_recording_and_playback | Запись И Воспроизведение Клонов | В MVP-срезе прошлая итерация записывает позицию/скорость и после отмотки становится playback-клоном; после конца записи клон удерживает последнюю позицию до конца таймера уровня; дефолт playback после вмешательства: `Preserve displacement`. | Позволяет строить решения через кооперацию с прошлым собой и физически менять последствия прошлой попытки. | MVP | time_rewind | all | validated_in_local_slice | docs/2026-07-04_clone_recording_and_playback_gd-spec.md |
| clone_collision_and_interaction | Коллизии И Взаимодействие Клонов | Клоны взаимодействуют с игроком и объектами; `Preserve displacement` выбран MVP-дефолтом, а сравнение с recover-режимом остается в editor/development debug-панели. | Дает нестандартные решения и делает клонов физической частью пазла. | MVP | clone_recording_and_playback | all | validated_in_local_slice | docs/2026-06-25_clone_collision_and_interaction_gd-spec.md |
| delayed_clone_control | Задержка Управления Новым Клоном | Новая управляемая итерация появляется после линейной задержки; после rewind текущая итерация скрывается, а в spawn-точке виден ghost/preview с радиальным progress-индикатором без текста. | Сохраняет читаемость старта, предотвращает разрушение предыдущих записей и объясняет задержку как часть таймлайна. | MVP | clone_recording_and_playback | all | validated_in_local_slice | docs/2026-07-05_delayed_clone_control_gd-spec.md |
| clone_readability | Визуальная Читаемость Клонов | Активный игрок самый заметный, playback-клоны получают alpha по возрасту, altered-клоны получают flash и слабый tint/outline поверх age-alpha; текущие цвета вынесены в visual config. | Помогает понимать, какая итерация активна, в каком порядке идут клоны и где игрок вмешался в прошлое. | MVP | clone_recording_and_playback | all | validated_in_local_slice | docs/2026-07-05_clone_readability_gd-spec.md |
| buttons_levers_doors | Кнопки, Рычаги И Двери | В MVP-срезе реализована нажимная плита, открывающая дверь, которую может удерживать playback-клон. | Создает основной язык puzzle-задач. | MVP | player_movement, clone_collision_and_interaction | all | validated_in_webgl_slice | TBD |
| elevators | Лифты | Вертикальные или горизонтальные платформы, управляемые кнопками, рычагами или таймингом. | Добавляет позиционные задачи и маршруты для клонов. | MVP | buttons_levers_doors | all | planned | TBD |
| basic_traps | Базовые Ловушки | В MVP-срезе есть pit hazard как placeholder-ловушка; стационарные/выдвижные шипы и диски остаются будущей задачей. | Дает угрозы, вокруг которых строятся временные синхронизации. | MVP | death_and_paradox, buttons_levers_doors | all | implemented_in_mvp_slice | TBD |
| level_restart | Рестарт Уровня | В MVP-срезе restart доступен из HUD после fail-state. | Снижает фрустрацию и возвращает контроль над ситуацией. | MVP | level_timer_and_exit, time_rewind | all | implemented_in_mvp_slice | TBD |
| tutorial_onboarding | Обучение | Последовательные уровни, объясняющие движение, отмотку, клонов, парадокс и объекты. | Делает сложную core-фичу понятной без перегруза. | MVP | player_movement, time_rewind, buttons_levers_doors, basic_traps | all | planned | TBD |
| handcrafted_campaign | Ручная Кампания | Последовательность вручную созданных комнат, построенных вокруг временных клонов. | Дает долгосрочную цель и управляемую кривую сложности. | MVP | tutorial_onboarding, basic_traps, elevators | premium | planned | TBD |
| star_medal_progression | Звезды И Медали | Уровни оцениваются звездами/медалями, а следующие зоны открываются по суммарному прогрессу. | Добавляет replay-value и понятный мета-прогресс кампании. | P1 | handcrafted_campaign, level_timer_and_exit | premium | planned | TBD |
| achievements | Достижения | Ачивки за прохождение, сложные решения, скорость или чистое выполнение. | Дает дополнительную мотивацию вне обязательного прохождения. | P1 | handcrafted_campaign, star_medal_progression | premium | planned | TBD |
| advanced_traps | Продвинутые Ловушки | Лазеры, датчики движения и более сложные угрозы. | Расширяет вариативность задач после освоения базовых систем. | P2 | basic_traps, handcrafted_campaign | premium | planned | TBD |
| light_mechanics | Свет И Датчики Света | Механики света, теней или датчиков, связанные с видимостью и активацией объектов. | Дает новый слой puzzle-дизайна для позднего контента или DLC. | P2 | advanced_traps | premium | planned | TBD |
| dlc_content_packs | DLC-Контент | Дополнительные наборы уровней и механик после релиза. | Продлевает жизнь игры без влияния на MVP. | P2 | handcrafted_campaign, advanced_traps | premium | planned | TBD |
| mobile_adaptation | Мобильная Адаптация | Отдельная адаптация интерфейса, управления и читаемости под мобильные устройства. | Расширяет аудиторию после проверки PC-версии. | P2 | player_movement, tutorial_onboarding, handcrafted_campaign | premium | planned | TBD |

## Dependency Map

- `player_movement` -> `level_timer_and_exit` -> `death_and_paradox`
- `death_and_paradox` + `level_timer_and_exit` -> `time_rewind`
- `time_rewind` -> `clone_recording_and_playback`
- `clone_recording_and_playback` -> `clone_collision_and_interaction`
- `clone_recording_and_playback` -> `delayed_clone_control`
- `clone_recording_and_playback` -> `clone_readability`
- `clone_collision_and_interaction` -> `buttons_levers_doors`
- `buttons_levers_doors` -> `elevators`
- `death_and_paradox` + `buttons_levers_doors` -> `basic_traps`
- `time_rewind` + `level_timer_and_exit` -> `level_restart`
- `player_movement` + `time_rewind` + `buttons_levers_doors` + `basic_traps` -> `tutorial_onboarding`
- `tutorial_onboarding` + `basic_traps` + `elevators` -> `handcrafted_campaign`
- `handcrafted_campaign` -> `star_medal_progression`
- `star_medal_progression` -> `achievements`
- `basic_traps` + `handcrafted_campaign` -> `advanced_traps`
- `advanced_traps` -> `light_mechanics`
- `handcrafted_campaign` + `advanced_traps` -> `dlc_content_packs`
- `handcrafted_campaign` + `tutorial_onboarding` -> `mobile_adaptation`

## Status Legend

- `planned`: фича еще не реализована.
- `implemented_in_mvp_slice`: реализовано минимальное подмножество для текущего вертикального среза, но без отдельной полной feature spec или полного QA.
- `validated_in_webgl_slice`: реализовано и подтверждено ручной проверкой в опубликованной WebGL-сборке 2026-06-25.
- `validated_in_local_slice`: реализовано и подтверждено пользователем в локальной/текущей playable-версии этой сессии.
- `updated_pending_unity_validation`: дизайн и локальные файлы обновлены, но требуется проверка в Unity Editor / playable build.
- `mvp_placeholder`: есть временное решение для читаемости/проверки core loop, финальная реализация требует отдельной работы.

## Documentation Registry

| doc | purpose | current_status |
|---|---|---|
| `docs/project-concept.md` | Верхнеуровневое видение проекта, core loop, MVP границы и открытые вопросы. | updated_for_validated_local_slice |
| `docs/feature-index.md` | Реестр фич, статусов, зависимостей и документации. | updated_for_validated_local_slice |
| `docs/2026-06-25_time_rewind_gd-spec.md` | Feature spec центральной механики отмотки времени и вертикального среза. | validated_in_local_slice |
| `docs/2026-06-25_player_movement_gd-spec.md` | Feature spec движения и прыжка для MVP-среза. | validated_in_webgl_slice |
| `docs/2026-07-04_clone_recording_and_playback_gd-spec.md` | Feature spec записи и воспроизведения клонов, включая гибридный источник истины, default `Preserve displacement` и удержание клона до конца таймера. | validated_in_local_slice |
| `docs/2026-06-25_clone_collision_and_interaction_gd-spec.md` | Feature spec физического взаимодействия клонов и debug-режимов смещения. | validated_in_local_slice |
| `docs/2026-07-05_delayed_clone_control_gd-spec.md` | Feature spec задержки появления новой итерации, ghost/preview и радиального spawn-индикатора. | validated_in_local_slice |
| `docs/2026-07-05_clone_readability_gd-spec.md` | Feature spec визуальной читаемости активного игрока, playback-клонов, altered-состояния и ghost. | validated_in_local_slice |
| `docs/unity-project-setup.md` | Unity setup, локальная проверка, WebGL build и GitHub Pages deploy. | updated_for_validated_local_slice |
| `docs/DECISION_LOG.md` | Журнал принятых решений по Unity, фичам, WebGL и публикации. | updated_for_validated_local_slice |
| `docs/git-workflow.md` | Git workflow проекта. | unchanged_this_session |

## Top-3 Рисковые Фичи

1. `clone_collision_and_interaction`
   - Риск: взаимодействие клонов друг с другом может порождать хаос, неожиданные блокировки и нечитаемые решения.
   - Почему важно: это центральная особенность, отличающая игру от обычного платформера-головоломки.

2. `handcrafted_campaign`
   - Риск: каждый уровень должен быть спроектирован вручную вокруг временных клонов, без ощущения случайного лабиринта.
   - Почему важно: игра держится на качестве puzzle-комнат и ясности маршрута.

3. `clone_recording_and_playback`
   - Риск: если запись действий, воспроизведение и синхронизация ощущаются неточно, игрок перестанет доверять системе.
   - Почему важно: игрок должен понимать, что прошлые действия воспроизводятся честно и предсказуемо.

## Предложенный Порядок Разработки

1. `player_movement`
2. `level_timer_and_exit`
3. `death_and_paradox`
4. `time_rewind`
5. `clone_recording_and_playback`
6. `delayed_clone_control`
7. `clone_readability`
8. `clone_collision_and_interaction`
9. `buttons_levers_doors`
10. `elevators`
11. `basic_traps`
12. `level_restart`
13. `tutorial_onboarding`
14. `handcrafted_campaign`
15. `star_medal_progression`
16. `achievements`
17. `advanced_traps`
18. `light_mechanics`
19. `dlc_content_packs`
20. `mobile_adaptation`

## Рекомендуемая Первая Feature Spec

Рекомендуемый первый документ: `time_rewind`.

Причина: эта фича связывает таймер уровня, смерть/парадокс, запись действий, появление клонов и основное отличие игры. После ее фиксации проще специфицировать `clone_recording_and_playback`, `clone_collision_and_interaction` и объекты головоломок.
