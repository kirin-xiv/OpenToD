# FFSlots Dragon Link Edition - Product Requirements Document

## Project Overview

**Product Name:** FFSlots Dragon Link Edition  
**Project Type:** FFXIV Plugin Enhancement  
**Target Audience:** FFXIV venue operators and roleplaying communities  
**Development Timeline:** 8-12 weeks  
**Technical Framework:** Dalamud.NET.Sdk with ECommons and Hexa.NET.ImGui

## Vision Statement

Transform FFSlots into an immersive Final Fantasy XIV themed slot experience inspired by Dragon Link's innovative mechanics, featuring Crystal Resonance (Hold & Spin), progressive jackpots, and spectacular visual/audio effects that integrate seamlessly with FFXIV's rich lore and aesthetic.

## Core Features

### 1. Crystal Resonance System (Hold & Spin Equivalent)

**Trigger Condition:**
- 6+ Crystal symbols anywhere on the reels during base game
- Crystal symbols lock in place and display gil values

**Mechanics:**
- Player receives 3 respins with locked Crystals
- Landing new Crystals resets respins to 3
- Each Crystal shows randomized gil values (50-10,000 based on bet)
- Special "Jackpot Crystals" can appear with MINI/MINOR/MAJOR/GRAND symbols
- Feature ends when respins expire or all 15 positions filled

**FFXIV Integration:**
- Crystals pulse with aetheric energy effects
- Sound effects from FFXIV crystal interactions
- Visual: Floating crystals with Eorzean runic inscriptions

### 2. Progressive Jackpot System

**Four-Tier Structure:**
- **MINI:** Starts at 10,000 gil, increments by 0.5% of each bet
- **MINOR:** Starts at 50,000 gil, increments by 0.3% of each bet  
- **MAJOR:** Starts at 250,000 gil, increments by 0.15% of each bet
- **GRAND:** Starts at 1,000,000 gil, increments by 0.05% of each bet

**Winning Conditions:**
- MINI: Fill bottom row during Crystal Resonance
- MINOR: Fill middle 3 rows during Crystal Resonance
- MAJOR: Fill top 4 rows during Crystal Resonance  
- GRAND: Fill all 15 positions during Crystal Resonance

**FFXIV Theming:**
- MINI: Copper coin with Ul'dahn markings
- MINOR: Silver coin with Limsan design
- MAJOR: Gold coin with Gridanian emblems
- GRAND: Platinum coin with Ishgardian dragons

### 3. Realm Expedition Free Spins

**Trigger:** 3+ Portal symbols on reels 1, 3, 5
**Selection Phase:** Player chooses expedition destination:

**Available Realms:**
- **Thanalan Expedition** (High volatility)
  - 10 free spins, 3x multipliers possible
  - Desert theme with sandworm encounters
  
- **Black Shroud Journey** (Medium volatility)  
  - 15 free spins, 2x multipliers
  - Forest theme with elemental spirits
  
- **La Noscea Voyage** (Low volatility)
  - 20 free spins, steady wins
  - Ocean theme with pirate treasures

**Special Features:**
- Each realm has unique background music from FFXIV
- Expedition-specific wild symbols with realm creatures
- Chance for "Primal Encounters" - instant win bonuses

### 4. FFXIV Symbol Integration

**Base Symbols (Low-Medium Pay):**
- Gil Coins, Potions, Crystals, Chocobo Feathers
- Moogle Dolls, Cactuar Needles, Mandragora Sprouts

**High Pay Symbols:**
- Job Crystals (WAR, WHM, BLM, etc.)
- Primal Emblems (Ifrit, Shiva, Bahamut)
- Relic Weapons (Excalibur, Ragnarok, Apocalypse)

**Special Symbols:**
- **Wild:** Crystal of Light (substitutes for all symbols except special)
- **Portal:** Aetheryte Crystal (triggers free spins)
- **Crystal:** Materia (triggers Hold & Spin)

### 5. Audio-Visual Spectacular

**Sound Design:**
- FFXIV sound effects for all interactions
- Nobuo Uematsu inspired win celebration music
- Primal roars for major wins
- Aetheric humming for ambient atmosphere

**Visual Effects:**
- Particle effects for wins using FFXIV spell aesthetics
- Crystal resonance creates aetheric field overlays
- Jackpot wins trigger screen-wide light explosions
- Reel symbols animate with job-specific abilities

## Technical Requirements

### Architecture Enhancements

**New Components:**
- `CrystalResonanceManager`: Handles Hold & Spin logic
- `ProgressiveJackpotSystem`: Manages four-tier jackpots
- `RealmExpeditionManager`: Controls free spin features  
- `FFXIVAudioManager`: Integrates game sound effects
- `ParticleEffectRenderer`: Creates visual spectacles

**Data Persistence:**
- Jackpot values saved to configuration
- Player progression tracking
- Session-based feature triggers
- Cross-session jackpot accumulation

**Performance Optimization:**
- Efficient particle system using GPU acceleration
- Audio streaming for seamless music transitions
- Optimized animation loops for smooth 60fps gameplay

### Integration Points

**FFXIV Game Client:**
- Sound effect extraction and playback
- Font rendering matching FFXIV UI
- Color schemes from game's visual design
- Particle effect system inspiration

**Venue Management:**
- Operator controls for jackpot seeding
- Feature frequency adjustment
- Real-time analytics dashboard
- Player session comprehensive tracking

## Development Phases

### Phase 1: Crystal Resonance Foundation (Weeks 1-3)
- Implement basic Hold & Spin mechanics
- Create Crystal symbol logic and visual design
- Build respin counter and position locking
- Basic gil value assignment system

### Phase 2: Progressive Jackpots (Weeks 4-5)  
- Four-tier jackpot accumulation system
- Triggering logic during Crystal Resonance
- Jackpot display and celebration sequences
- Cross-session persistence

### Phase 3: Realm Expeditions (Weeks 6-7)
- Free spin selection interface
- Three realm variants with unique features
- FFXIV location theming and music
- Expedition-specific bonus mechanics

### Phase 4: Audio-Visual Polish (Weeks 8-10)
- FFXIV sound effect integration
- Particle effect system implementation
- Animation sequences for major wins
- UI polish and FFXIV aesthetic matching

### Phase 5: Testing and Optimization (Weeks 11-12)
- Comprehensive feature testing
- Performance optimization
- Venue operator feedback integration
- Launch preparation and documentation

## Success Metrics

**Player Engagement:**
- Average session duration increase of 150%
- Feature trigger rate optimization (Crystal Resonance: 1 in 200 spins)
- Player retention week-over-week improvement

**Venue Adoption:**
- 90% of existing FFSlots venues upgrade within 30 days
- New venue adoption rate increase of 200%
- Positive operator feedback scores (4.5+/5.0)

**Technical Performance:**
- Maintain 60fps during all animations
- Loading times under 2 seconds for feature transitions
- Zero crashes related to new features

## Risk Assessment and Mitigation

**Technical Risks:**
- Performance impact from particle effects → GPU acceleration and optimization
- Audio integration complexity → Modular audio system with fallbacks
- Memory usage from enhanced graphics → Efficient asset management

**User Experience Risks:**
- Feature complexity overwhelming new players → Tutorial system and simplified mode
- Balance concerns with jackpot frequency → Configurable operator controls
- FFXIV theme accuracy → Community feedback integration

## Launch Strategy

**Beta Testing Phase:**
- Invite 10 established venues for 2-week beta
- Collect comprehensive feedback on all features
- Performance testing across various hardware configurations

**Soft Launch:**
- Release to existing FFSlots user base
- Monitor jackpot accumulation and payout rates
- Gather operator feedback on management tools

**Full Launch:**
- Community announcement with feature showcase
- Documentation and tutorial content release
- Ongoing support and feature enhancement roadmap

---

**Document Version:** 1.0  
**Last Updated:** November 10, 2025  
**Prepared By:** Claude Code Assistant  
**Status:** Approved for Development