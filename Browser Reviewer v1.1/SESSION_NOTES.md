# Browser Reviewer v0.2 Session Notes

## Punto exacto

`Browser Reviewer v0.2` quedó como base del release `v1.0`.

Estado:

- build funcional
- QA funcional
- carpeta canónica de ejecución definida
- release candidate validado con evidencia real
- GUI y CLI alineados sobre el mismo motor principal

## Carpeta canónica

- `C:\Users\gustavo\Desktop\Desarrollo\Browser-Reviewer\publish\BrowserReviewer-current`

Usar solo esa carpeta para pruebas finales.

## Cambios técnicos más importantes ya integrados

### Extracción

- parsers separados por artefacto
- fallback Firefox mejorado para archivos bloqueados
- deduplicación por fuente procesada
- temporales Firefox limpiados y excluidos
- `File` real en cache

### Identidad de navegador

- familia técnica por estructura:
  - `Firefox-like`
  - `Chromium-like`
- nombre visual por path conocido
- fallback controlado para desconocidos
- soporte de hosts conocidos:
  - DeepL
  - Outlook WView2
  - Visual Studio WView2
  - Windows Search WView2
  - Roblox Studio WView2
  - TeamViewer WView2
  - Amazon WorkSpaces WView2
  - Steam Embedded Chromium
  - OneDrive WView2
  - OpenAI Codex WView2

### Autofill

- Firefox autofill ya no se guarda como Chrome
- Chrome autofill clásico vuelve a salir
- parser estructurado Chromium ya no rompe el clásico

### Log

- ruido de `Skipping already processed ...` reducido a resumen final

### CLI / GUI

- `CLI_ListFilesAndDirectories(...)` usa `ListFilesAndDirectories(...)`
- se eliminó `CLI_CopyAndProcessToTemp(...)`
- ya no queda un fallback alterno solo para CLI

## Evidencia de referencia

### Mejor caso local actual

- `C:\Users\gustavo\Desktop\Browser Reviewer Works\5FinalLocal.bre`

Resultado:

- `validate-db`: `PASS`
- `heuristic-audit`: `PASS`

Valores guía:

- `firefox_results`: `86124`
- `autofill_data | Firefox`: `1855`
- `autofill_data | Chrome`: `787`
- `cookies_data | Firefox`: `1762`
- `cache_data | Firefox`: `29840`
- `local_storage_data | OpenAI Codex WView2`: `11`

### Validación de contaminación

`DESKTOP-V51HBR1`:

- sí aparece en casos locales
- no aparece en casos `AX200`

Conclusión:

- el hostname local no se está inyectando globalmente
- cuando aparece, pertenece a la evidencia

## Commit de respaldo

- `6c07d23`
- `Stabilize release candidate and update browser branding`

## Qué falta

Poco.

Siguiente paso razonable:

1. decidir congelación `v1.0`
2. si se desea, hacer commit final
3. conservar `publish\BrowserReviewer-current` como build canónica
