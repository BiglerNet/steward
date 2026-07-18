# Changelog

## [1.0.0](https://github.com/BiglerNet/steward/compare/v0.1.0...v1.0.0) (2026-07-18)


### ⚠ BREAKING CHANGES

* replace service records with maintenance items, templates, and kanban tracking ([#13](https://github.com/BiglerNet/steward/issues/13))
* split engine mechanism from fuel type and generalize fuel logs for EVs ([#11](https://github.com/BiglerNet/steward/issues/11))
* asset, registration, and asset-type-registry API contracts changed (category-based asset creation/filtering, registration kind is now required, photo/cover-photo fields replace PhotoUrl, refresh tokens are now required for session handling). EF Core migrations were reset to a fresh InitialCreate; this is pre-launch so no data migration is needed.

### Features

* added google single sign on ([#4](https://github.com/BiglerNet/steward/issues/4)) ([7df9903](https://github.com/BiglerNet/steward/commit/7df99039a91133f5339f4742c85ad5c9e6c94a9f))
* adopt biglernet design system ([#2](https://github.com/BiglerNet/steward/issues/2)) ([6d0328b](https://github.com/BiglerNet/steward/commit/6d0328bedda36a10f549d20cfb6c0e0b9bf4bdb5))
* first lol ([0c4af97](https://github.com/BiglerNet/steward/commit/0c4af97ce30339b5af06be1a904a29c21fee06f9))
* replace service records with maintenance items, templates, and kanban tracking ([#13](https://github.com/BiglerNet/steward/issues/13)) ([4e28cc9](https://github.com/BiglerNet/steward/commit/4e28cc9bb2654d559c76b870191ab22b7fd5f56d))
* rework asset types, registrations, photos, dashboard editing, and auth sessions ([#8](https://github.com/BiglerNet/steward/issues/8)) ([d71632e](https://github.com/BiglerNet/steward/commit/d71632ead25d08c3ee9461a0b6d9c7be246e2b34))
* split engine mechanism from fuel type and generalize fuel logs for EVs ([#11](https://github.com/BiglerNet/steward/issues/11)) ([5135113](https://github.com/BiglerNet/steward/commit/513511336cb07e94574849c0dec6bbdc4907422a))
* updating deployment process to use secrets ([56791e8](https://github.com/BiglerNet/steward/commit/56791e8dee2a14379dc404d52f8b4c20a9c5bde6))


### Bug Fixes

* added missing health checks ([#3](https://github.com/BiglerNet/steward/issues/3)) ([2da9b11](https://github.com/BiglerNet/steward/commit/2da9b1104c71756249399d8bd5323b7e9781e224))
* added support for x-forwarded-* headers ([#7](https://github.com/BiglerNet/steward/issues/7)) ([1305677](https://github.com/BiglerNet/steward/commit/130567794618f9f49b08bdcb62b93f8109c76c71))
* another qemu fix attempt ([b8a43b5](https://github.com/BiglerNet/steward/commit/b8a43b596b3dc6b969c153748dcdfe03e8550f06))
* build multi-arch images and fix pgBackRest repo requirement ([99e5a10](https://github.com/BiglerNet/steward/commit/99e5a1024ee6ae7371606beb7b21cba0b457545a))
* changed how connection string is built to handle issues with escape characters in passwords ([99f6b61](https://github.com/BiglerNet/steward/commit/99f6b61199fabac7797bc75d4796326eb014bc25))
* changed schema to new 'steward' instead of public ([8f5aef7](https://github.com/BiglerNet/steward/commit/8f5aef7e6256cef365bca936dec157873af9b512))
* corrected invalid scheme detection and rely on base url when deployed ([#6](https://github.com/BiglerNet/steward/issues/6)) ([a4cecfc](https://github.com/BiglerNet/steward/commit/a4cecfc7a493b3688c50066761d3953f3760fd85))
* corrected race condition during setup ([066c805](https://github.com/BiglerNet/steward/commit/066c80599397e60e00ea543b5793631b643cb29a))
* correcting qemu build errors ([92f1323](https://github.com/BiglerNet/steward/commit/92f1323f9dcd66ae4306c71e0ba484e1c88250f6))
* corrects base API url in frontend container deploy ([#5](https://github.com/BiglerNet/steward/issues/5)) ([44fd90a](https://github.com/BiglerNet/steward/commit/44fd90a424594dadb03cb690c4c8cab36678b211))
* dialogs now have proper overflow scroll handling ([2eef100](https://github.com/BiglerNet/steward/commit/2eef100fbfe803c451567c9d2bd6af827a1a919a))
* fixed build errors due to vulnerabilities and upgraded to xunit v3 ([6f6ade5](https://github.com/BiglerNet/steward/commit/6f6ade5a25ff43e10ce9da3bd18fd7db68d0135f))
* fixed failing startup job due to race condition ([d3b7a0d](https://github.com/BiglerNet/steward/commit/d3b7a0de8b208a2390703fcc8630b2fdb0219819))
* more qemu fix attempts by splitting build jobs ([f15d0e5](https://github.com/BiglerNet/steward/commit/f15d0e5f46ad12c8f00d24977ae3cd0a7e347050))
* provision api file storage and guard against blank Storage:RootPath ([#9](https://github.com/BiglerNet/steward/issues/9)) ([9536ae6](https://github.com/BiglerNet/steward/commit/9536ae6cb1714828fa0dd85b299c71b7a731bbe1))
* removing create-namespace from call to handle SA scope properly ([1ce1bde](https://github.com/BiglerNet/steward/commit/1ce1bde955a61de76f7cc60d12e827deaaa3c4d2))
* **web:** stop dialogs closing when interacting with a nested Select dropdown ([#12](https://github.com/BiglerNet/steward/issues/12)) ([637eaec](https://github.com/BiglerNet/steward/commit/637eaecc3aeb518d777761cda7a2a8c1aeff20f7))


### Miscellaneous

* **branding:** fixed incorrect naming in some screens and added favicon ([#10](https://github.com/BiglerNet/steward/issues/10)) ([55e916e](https://github.com/BiglerNet/steward/commit/55e916e822e89aba488a9c5e0326985363b42498))
