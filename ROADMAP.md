# Unify MCP Development Roadmap

## Current Metadata

- Current Status: Work in Progress
- Version: 0.3.5
- Last Updated: 2026-02-11
- Canonical GitHub Project: [#40 unify-mcp](https://github.com/users/zachyzissou/projects/40)
- Project Refresh: 2026-05-31T06:40Z

## Honest Project Status

This roadmap provides a transparent view of what's implemented vs. what's planned. While the documentation system and context optimization are functional, **core MCP protocol handling and most tool implementations are incomplete**.

---

## ✅ What Works Today

### Documentation System (Functional)
- ✅ SQLite FTS5 indexing of Unity documentation
- ✅ Fuzzy search with Levenshtein distance
- ✅ HTML documentation parsing (AngleSharp)
- ✅ Unity version detection and installation discovery
- ✅ Documentation caching and incremental updates
- ✅ Deprecation detection

### Context Optimization (Functional)
- ✅ Token usage optimization (50-70% reduction)
- ✅ Request deduplication
- ✅ Tool result summarization
- ✅ Response caching
- ✅ Context-aware tool suggestions

### Infrastructure (Functional)
- ✅ Path validation and security checks
- ✅ Main thread dispatcher for Unity Editor
- ✅ Error handling framework
- ✅ Configuration management
- ✅ CI pipeline with .NET tests

---

## ❌ What's Not Implemented (TODOs/Stubs)

### MCP Protocol (Critical Gap)
- ❌ stdio transport initialization
- ❌ ModelContextProtocol server wiring
- ❌ Tool attribute processing ([McpServerToolType], [McpServerTool] commented out)
- ❌ Actual MCP protocol handling in McpServerLifecycle

### Tool Implementations (Stubbed JSON)
- ❌ BuildTools: Returns hardcoded JSON instead of actual builds
- ❌ AssetTools: All methods return stub data
- ❌ SceneTools: All methods return stub data
- ❌ ProfilerTools: Likely stubbed (needs verification)
- ❌ PackageTools: Implementation status unclear

### Security & Authorization
- ❌ Tool call authorization/permission gating
- ❌ Rate limiting
- ❌ Audit logging
- ❌ Role-based access control

### Testing & Quality
- ❌ Integration tests not wired into csproj (exist but not compiled)
- ❌ Unity Test Runner not in CI
- ❌ Performance tests not in CI
- ❌ Many test files excluded from build

---

## Development Phases

### Phase 1: Honesty & Alignment (Sprint 1-2)
**Goal**: Accurate representation of project state

**Deliverables**:
- [x] Create honest ROADMAP.md (this file)
- [ ] Update API_REFERENCE.md: Remove "Production Ready" status → "Work in Progress" (#3)
- [ ] Add implementation status badges to README.md
- [ ] Create IMPLEMENTATION_STATUS.md showing what works vs. stubs
- [ ] Fix version drift: Align package.json (0.3.5) with docs (0.1.0) (#7)
- [ ] Update CHANGELOG.md to reflect actual feature state

**Acceptance Criteria**:
- Documentation accurately reflects implementation state
- No misleading "production-ready" claims
- Users know what's usable vs. placeholder

**Issues**: #3, #7

---

### Phase 2: Core MCP Protocol (Sprint 3-6)
**Goal**: Implement actual MCP server functionality

**Prerequisites**: Phase 1 complete

**Deliverables**:
- [ ] Initialize stdio transport in McpServerLifecycle.Start() (#4)
- [ ] Wire ModelContextProtocol.dll to server lifecycle (#4)
- [ ] Uncomment and implement [McpServerToolType] attribute processing
- [ ] Uncomment and implement [McpServerTool] attribute processing
- [ ] Connect at least one tool (DocumentationTools) to MCP protocol
- [ ] Add MCP protocol conformance tests
- [ ] Test with Claude Desktop integration

**Acceptance Criteria**:
- MCP server actually starts and listens on stdio
- At least one tool callable via MCP protocol
- Tests verify protocol compliance
- Claude Desktop can connect and call tools

**Issues**: #4

**Estimated Effort**: 3-4 weeks (complex protocol integration)

---

### Phase 3: Complete Tool Implementations (Sprint 7-12)
**Goal**: Replace all stubbed JSON with real Unity API calls

**Prerequisites**: Phase 2 complete (tools need MCP wiring to be testable)

**Deliverables**:

#### BuildTools (#5)
- [ ] Implement StartMultiPlatformBuild with actual BuildPipeline API
- [ ] ValidateBuildConfiguration with real platform checks
- [ ] GetBuildSizeAnalysis with actual build reports

#### AssetTools (#5)
- [ ] Implement FindUnusedAssets with AssetDatabase queries
- [ ] AnalyzeAssetDependencies with real dependency graph
- [ ] OptimizeTextureSettings with actual texture import settings

#### SceneTools (#5)
- [ ] Implement ValidateScene with SceneManager + GameObject inspection
- [ ] FindMissingReferences with actual reference scanning
- [ ] AnalyzeLightingSetup with real lighting data

#### ProfilerTools (#5)
- [ ] Verify implementation status (may already be stubbed)
- [ ] Implement with Unity Profiler API if needed

**Acceptance Criteria**:
- No stubbed JSON responses remain
- All tools interact with actual Unity Editor APIs
- Integration tests verify real Unity operations
- Tools work in real Unity projects

**Issues**: #5

**Estimated Effort**: 4-6 weeks (requires Unity API expertise)

---

### Phase 4: Security & Authorization (Sprint 13-15)
**Goal**: Add production-grade security

**Prerequisites**: Phase 3 complete (working tools to secure)

**Deliverables**:
- [ ] Design IToolAuthorizationProvider interface (#6)
- [ ] Implement permission check in tool invocation pipeline
- [ ] Add RBAC configuration (YAML/JSON)
- [ ] Implement audit logging for tool calls
- [ ] Add rate limiting per-tool
- [ ] Create SECURITY.md documenting auth model
- [ ] Add security tests (unauthorized call blocking)

**Acceptance Criteria**:
- All tool calls pass through authorization
- Configuration defines who can call what
- Audit trail records all tool invocations
- Tests verify unauthorized calls blocked

**Issues**: #6

**Estimated Effort**: 2-3 weeks

---

### Phase 5: Testing & CI Quality (Sprint 16-18)
**Goal**: Comprehensive test coverage and CI validation

**Prerequisites**: Phase 3 complete (working code to test)

**Deliverables**:
- [ ] Enable all test files in csproj (#8)
- [ ] Wire integration tests into CI (#8)
- [ ] Wire performance tests into CI (#8)
- [ ] Add Unity Test Runner to GitHub Actions (#9)
- [ ] Add test coverage reporting
- [ ] Add Unity package import validation
- [ ] Test in Unity 2021.3 LTS and 2022.3 LTS
- [ ] Document test strategy in CONTRIBUTING.md

**Acceptance Criteria**:
- All test files compiled and run
- CI tests both .NET and Unity Editor
- Test coverage >= 80% for core components
- No skipped tests without documented reason

**Issues**: #8, #9

**Estimated Effort**: 2-3 weeks

---

## Phase 6: Optimization & Polish (Sprint 19+)
**Goal**: Production-ready refinement

**Prerequisites**: Phases 1-5 complete

**Deliverables**:
- [ ] Performance benchmarking suite
- [ ] Memory profiling and optimization
- [ ] Error message improvement
- [ ] Documentation polish
- [ ] Example projects / tutorials
- [ ] Migration guide (if breaking changes)
- [ ] 1.0.0 release preparation

**Acceptance Criteria**:
- No P0/P1 issues remain
- Performance meets documented targets
- Documentation is comprehensive
- Ready for production use

**Estimated Effort**: 3-4 weeks

---

## Timeline Estimate

| Phase | Duration | Completion |
|-------|----------|------------|
| Phase 1: Honesty & Alignment | 1-2 weeks | TBD |
| Phase 2: Core MCP Protocol | 3-4 weeks | TBD |
| Phase 3: Complete Tool Implementations | 4-6 weeks | TBD |
| Phase 4: Security & Authorization | 2-3 weeks | TBD |
| Phase 5: Testing & CI Quality | 2-3 weeks | TBD |
| Phase 6: Optimization & Polish | 3-4 weeks | TBD |
| **Total** | **15-22 weeks (~4-5 months)** | |

---

## Contributing

See [CONTRIBUTING.md](Documentation~/CONTRIBUTING.md) for development guidelines.

If you'd like to help accelerate this roadmap:
1. Check open issues tagged with the phase you want to work on
2. Comment on the issue to claim it
3. Follow the PR template for phase-related changes
4. Link your PR to the relevant issue

---

## Questions?

- **Why was this roadmap created?** To provide honest transparency about project status
- **Is the documentation system usable?** Yes, it's functional and tested
- **Can I use this in production?** Not recommended until Phase 2-3 complete (MCP protocol + tool implementations)
- **When will it be production-ready?** Estimated 4-5 months if development continues
- **Can I contribute?** Yes! See issues tagged by phase

---

## Version History

- **0.3.5** (2026-02-11): Honest ROADMAP.md created, issues filed
- **0.3.4** (earlier): Documentation system functional, tools stubbed
- **0.1.0** (earlier): Initial documentation claims (now corrected)
