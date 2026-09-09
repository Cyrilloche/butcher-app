# Specification Quality Checklist: Modification et fin de vie d'un produit

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-09
**Last validated**: 2026-09-09 (after /speckit-clarify, session 1)
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Constitution Alignment (v1.0.0)

- [x] I. Simplicité — libellés et messages en français, explication du verrouillage exigée (FR-006, FR-024)
- [x] II. Backend garant — verrouillage et garde-fous exigés côté serveur (FR-005, FR-012, FR-016)
- [x] III. Frontière contractuelle — la spec ne préjuge d'aucun couplage hors contrat
- [x] IV. Traçabilité — numéros de lot jamais réattribués (FR-013, SC-004) ; sorties de stock jamais perdues (SC-006) ; identifiants FR séquentiels sans suffixe
- [x] V. Vagues — périmètre Vague 1, journalisation de la suppression renvoyée en Vague 2

## Clarifications résolues (round 1)

| Point | Décision |
|---|---|
| Seuil de bascule vers « utilisé » | Le premier lot de production rattaché (FR-002), la soupape étant la suppression de lot |
| Réutilisation du code d'un produit désactivé | Refusée, unicité globale maintenue (FR-007), rouvrable si l'usage le demande |
| Désactivation avec du stock restant | Bloquée, assortie d'une action de solde en perte des unités restantes (FR-016 à FR-018) |

## Clarifications résolues (/speckit-clarify, session 2026-09-09)

| Point | Décision |
|---|---|
| Poids de la sortie produite par le solde | Poids pesé si l'unité est disponible, restant estimé si elle est entamée — convention identique au menu de sortie unité par unité (FR-020, FR-021) |
| Portée et type de l'action de solde | Sélection des unités par l'utilisatrice, type toujours « perte » ; autre type rouvrable si l'usage le demande (FR-018, FR-019) |
| Édition concurrente | Dernière écriture gagnante, aucun jeton de version, garde-fous revérifiés à l'enregistrement (FR-028, FR-029) |

## Notes

- Scope grew during clarification: production-batch deletion (User Story 3, FR-010 to FR-015) was
  added at the user's request as the escape hatch that makes the product freeze acceptable.
- Two behaviours in the spec contradict what the code does today and are deliberate changes, not
  descriptions: batch numbering currently counts existing rows, which would reissue a deleted
  number (FR-013), and deactivation currently has no stock guard (FR-016).
- Functional requirements were renumbered FR-001 to FR-030 after the clarification session, so the
  lettered identifiers (FR-017a and the like) no longer exist. Nothing outside the spec cites them yet.
- Spec is ready for `/speckit-plan`.
