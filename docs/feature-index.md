# Retro Timer: Feature Index

## Feature List

| feature_id | feature_name | brief | player_value | priority | depends_on | distribution | status | spec_doc |
|---|---|---|---|---|---|---|---|---|
| player_movement | Движение И Прыжок | Базовое платформерное управление персонажем на PC с клавиатурой и геймпадом. | Дает точный контроль в комнатах, где важны позиция и тайминг. | MVP | none | all | planned | TBD |
| level_timer_and_exit | Таймер И Выход | Уровень имеет лимит времени, выход, условия победы при достижении выхода любым клоном и game over, если после задержки нового клона остается меньше 1 секунды активного времени. | Создает давление времени, ясную цель комнаты и конечный предел полезных итераций. | MVP | player_movement | all | planned | TBD |
| death_and_paradox | Смерть И Парадокс | Смерть любого клона завершает уровень поражением из-за временного парадокса. | Делает каждую прошлую итерацию важной и поддерживает напряжение. | MVP | player_movement | all | planned | TBD |
| time_rewind | Отмотка Времени | Игрок может вручную вернуть уровень к старту, а при истечении таймера игра принудительно отматывает время, если у нового клона останется минимум 1 секунда активного времени. | Превращает ошибку, тупик или истечение времени в часть решения, пока таймер еще позволяет полезную итерацию. | MVP | level_timer_and_exit, death_and_paradox | all | planned | TBD |
| clone_recording_and_playback | Запись И Воспроизведение Клонов | Прошлые итерации повторяют действия игрока после отмотки. | Позволяет строить решения через кооперацию с прошлым собой. | MVP | time_rewind | all | planned | TBD |
| clone_collision_and_interaction | Коллизии И Взаимодействие Клонов | Клоны взаимодействуют с объектами и друг с другом, включая блокировку движения и предотвращение опасности. | Дает нестандартные решения и делает клонов физической частью пазла. | MVP | clone_recording_and_playback | all | planned | TBD |
| delayed_clone_control | Задержка Управления Новым Клоном | Новый управляемый клон получает контроль после задержки, зависящей от числа отмоток. | Сохраняет читаемость старта и предотвращает разрушение предыдущих записей. | MVP | clone_recording_and_playback | all | planned | TBD |
| clone_readability | Визуальная Читаемость Клонов | Возраст и принадлежность клонов считываются через прозрачность, яркость или другой визуальный слой. | Помогает понимать, какая итерация выполняет какое действие. | MVP | clone_recording_and_playback | all | planned | TBD |
| buttons_levers_doors | Кнопки, Рычаги И Двери | Базовые управляемые объекты для открытия, закрытия и синхронизации проходов. | Создает основной язык puzzle-задач. | MVP | player_movement, clone_collision_and_interaction | all | planned | TBD |
| elevators | Лифты | Вертикальные или горизонтальные платформы, управляемые кнопками, рычагами или таймингом. | Добавляет позиционные задачи и маршруты для клонов. | MVP | buttons_levers_doors | all | planned | TBD |
| basic_traps | Базовые Ловушки | Стационарные шипы, выдвижные шипы и подвижные диски на траектории. | Дает угрозы, вокруг которых строятся временные синхронизации. | MVP | death_and_paradox, buttons_levers_doors | all | planned | TBD |
| level_restart | Рестарт Уровня | Игрок может полностью сбросить уровень при хаосе, ошибке или неудачном таймлайне. | Снижает фрустрацию и возвращает контроль над ситуацией. | MVP | level_timer_and_exit, time_rewind | all | planned | TBD |
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
