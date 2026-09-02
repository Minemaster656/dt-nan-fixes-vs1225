# NanGuard — Build Notes

## Проблемы при сборке и их решения

### 1. `dotnet restore` зависает
**Симптом:** `dotnet restore` зависал навечно на шаге "Determining projects to restore...".
**Причина:** nuget.org недоступен напрямую (нужен прокси).
**Решение:** Передавать прокси через переменные окружения:
```bash
http_proxy=http://127.0.0.1:9060 https_proxy=http://127.0.0.1:9060 ALL_PROXY=socks5://127.0.0.1:9050 dotnet build -c Release
```
Также можно прописать в `~/.config/opencode/AGENTS.md`.

### 2. `0Harmony.dll` лежит не в корне игры
**Симптом:** `CS0246: The type or namespace name 'HarmonyLib' could not be found`.
**Причина:** `0Harmony.dll` находится по пути `Lib/0Harmony.dll`, а не в корне.
**Решение:** Правильный `HintPath` в `.csproj`:
```xml
<HintPath>/path/to/vintagestory/Lib/0Harmony.dll</HintPath>
```

### 3. `EntityBehaviorControlledPhysics` не в VintagestoryAPI.dll
**Симптом:** `CS0246: The type or namespace name 'EntityBehaviorControlledPhysics' could not be found`.
**Причина:** Класс находится в `Mods/VSEssentials.dll`, а не в `VintagestoryAPI.dll` или `VintagestoryLib.dll`.
**Решение:** Добавить ссылку на `VSEssentials.dll` в `.csproj`:
```xml
<Reference Include="VSEssentials">
  <HintPath>/path/to/vintagestory/Mods/VSEssentials.dll</HintPath>
</Reference>
```

### 4. Неправильные `using` для типов API
**Симптом:** `CS0246` для `ICoreClientAPI`, `EntityPos`.
**Причина:** Нужны дополнительные namespace:
- `ICoreClientAPI` → `Vintagestory.API.Client`
- `EntityPos` → `Vintagestory.API.Common.Entities`
- `ModSystem` → `Vintagestory.API.Common`

### 5. Harmony `AccessTools.Field` не находит унаследованное поле
**Симптом:** Потенциальная ошибка `FieldInfo` будет `null`.
**Причина:** `LastUpdateMilliseconds` объявлено в `GameTickListenerBase`, а не в `GameTickListener`. `AccessTools.Field` ищет только в указанном типе.
**Решение:** Обращаться к полю напрямую через `__instance.LastUpdateMilliseconds` (оно публичное и унаследованное).

### 6. Harmony Finalizer должен совпадать с возвращаемым типом метода
**Симптом:** `GetNearestBlockSoundSource` возвращает `Block`, а не `IWorldSoundSource`.
**Причина:** Неверное предположение о типе возврата.
**Решение:** Finalizer должен иметь `ref Block __result` и присваивать `null` при подавлении исключения.

## Структура проекта
```
NanGuard/
├── NanGuard.csproj
├── NanGuardMod.cs
├── modinfo.json
└── .gitignore
```

## Как собрать заново
```bash
cd /home/minemaster/dev/nocrush-vs/NanGuard
http_proxy=http://127.0.0.1:9060 https_proxy=http://127.0.0.1:9060 ALL_PROXY=socks5://127.0.0.1:9050 dotnet build -c Release
cp bin/Release/net10.0/NanGuard.dll ~/.config/VintagestoryData/Mods/NanGuard/
```

## Патчи
1. **Prefix → `GameTickListener.OnTriggered`** — dt > 1с → cap 0.1с. NaN/Inf → 0.05с
2. **Prefix → `EntityPlayer.OnGameTick`** — NaN в Pos → ресет. NaN в Motion → zero
3. **Prefix → `EntityBehaviorControlledPhysics.ApplyTests`** — NaN в motion → zero
4. **Finalizer → `EntityPlayer.GetNearestBlockSoundSource`** — catch ArithmeticException → return null
