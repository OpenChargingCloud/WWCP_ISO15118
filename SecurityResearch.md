
# ISO/IEC 15118 Security Research

## Deployment Measurements and Real-World Exposure

- [Current Affairs: A Security Measurement Study of CCS EV Charging Deployments](https://arxiv.org/abs/2404.06635)  
  Large-scale measurement study of public CCS DC charging deployments, including protocol version support, TLS deployment, HomePlug Green PHY modem firmware age, and ISO 15118 PKI design weaknesses.

- [PIBuster: Exploiting a Common Misconfiguration in CCS EV Chargers](https://www.usenix.org/conference/vehiclesec25/presentation/szakaly)  
  Practical attack against Qualcomm HomePlug Green PHY modems used in CCS chargers and vehicles. Shows how a common PIB configuration can allow remote read/write of critical modem parameters.

## SLAC, SDP, HomePlug Green PHY and Man-in-the-Middle Attacks

- [CVE-2025-12357: ISO 15118-2 Improper Restriction of Communication Channel to Intended Endpoints](https://www.cve.org/CVERecord?id=CVE-2025-12357)  
  Vulnerability record for SLAC spoofing attacks that can enable a man-in-the-middle between EV and EVSE.

- [SwRI identifies security vulnerability in EV charging protocol](https://www.swri.org/newsroom/press-releases/swri-identifies-security-vulnerability-ev-charging-protocol)  
  Public disclosure summary of SLAC-based attacks against ISO 15118 charging communication, including wired and wireless spoofing experiments.

- [DCeption: Real-world Wireless Man-in-the-Middle Attacks Against CCS EV Charging](https://arxiv.org/abs/2601.15515)  
  Demonstrates wireless HomePlug Green PHY MitM attacks against CCS, including TLS stripping, protocol downgrade manipulation, and safety-relevant message manipulation.

- [Charging Communication Sniffing and Man-in-the-Middle Attacks](https://dl.acm.org/doi/10.1145/3679240.3734648)  
  Practical work on low-cost sniffing and MitM setups for EV charging communication, including SLAC, SDP, and IPv6 Neighbor Advertisement based attack paths.

- [V2G Injector: Whispering to cars and charging units through the Power-Line](https://www.sstic.org/media/SSTIC2019/SSTIC-actes/v2g_injector_playing_with_electric_cars_and_chargi/SSTIC2019-Article-v2g_injector_playing_with_electric_cars_and_charging_stations_via_powerline-dudek.pdf)  
  Early practical attack and tooling work for V2G communication over powerline, including protocol negotiation and MitM-relevant manipulation.

- [Brokenwire: Wireless Disruption of CCS Electric Vehicle Charging](https://arxiv.org/abs/2202.02104)  
  Wireless disruption attack against CCS by exploiting HomePlug Green PHY behavior used by DIN 70121 and ISO 15118.

- [Securing EV Charging System against Physical-layer Signal Injection Attacks](https://www.ndss-symposium.org/wp-content/uploads/vehiclesec2024-79-paper.pdf)  
  Defensive work against physical-layer signal injection attacks on EV charging communication.

## ISO 15118 Protocol and Plug-and-Charge Security

- [A threat analysis of the vehicle-to-grid charging protocol ISO 15118](https://link.springer.com/article/10.1007/s00450-017-0342-y)  
  Classic ISO 15118 threat analysis covering attacker models, protocol assumptions, and early security concerns.

- [Study on Analysis of Security Vulnerabilities and Countermeasures in ISO/IEC 15118 Based Electric Vehicle Charging Technology](https://doi.org/10.1109/ICITCS.2014.7021815)  
  Early ISO/IEC 15118 vulnerability and countermeasure analysis.

- [Security Aspects of ISO 15118 Plug and Charge Payment](https://arxiv.org/abs/2512.15966)  
  Detailed review of ISO 15118 security controls and Plug-and-Charge payment weaknesses, including a relay-style billing attack.

- [Streamlining Plug-and-Charge Authorization for Electric Vehicles with OAuth2 and OIDC](https://arxiv.org/abs/2501.14397)  
  Alternative authorization approach for ISO 15118 Plug-and-Charge using OAuth Device Authorization Grant, Rich Authorization Requests, and OpenID Connect.

- [Integrating Privacy into the Electric Vehicle Charging Architecture](https://petsymposium.org/popets/2022/popets-2022-0066.pdf)  
  Privacy-preserving charging authorization and billing architecture using TPM-based Direct Anonymous Attestation concepts.

- [QuantumCharge: Post-Quantum Cryptography for Electric Vehicle Charging](https://eprint.iacr.org/2023/430)  
  Post-quantum cryptography extension for ISO 15118 Plug-and-Charge, including migration, crypto-agility, and HSM considerations.

## ISO 15118-20 and Hardening Proposals

- [Enhancing Security in the ISO 15118-20 EV Charging System](https://www.sciencedirect.com/science/article/pii/S277315372500012X)  
  Proposes security improvements for ISO 15118-20 DC fast charging systems, including cyber-physical security and remote attestation concepts.

- [Security Weaknesses in ISO 15118-Based CCS2 Charging](https://link.springer.com/chapter/10.1007/978-981-95-3185-1_11)  
  Empirical security analysis of ISO 15118-based CCS2 charging using custom EVCC and SECC implementations.

## Fuzzing, Testing and Implementation Robustness

- [Finding Bugs in ISO15118 Implementations with EVFUZZ](https://beerkay.github.io/papers/Berkay2024EVFUZZDemoVehicleSec.pdf)  
  ISO 15118 fuzzing approach using an EVSE-side test setup to identify implementation bugs.

- [V2G Fuzzer: Fuzzing Tool for Implementing Electric Vehicle Charging Communication](https://easychair.org/publications/preprint/CFCB/open)  
  State-machine-aware fuzzing approach for ISO 15118 message sequences.

- [Comprehensive Black-Box Fuzzing of Electric Vehicle Charging Firmware via a Vehicle to Grid Network Protocol Based on State Machine Path](https://www.techscience.com/cmc/v84n2/62867)  
  Black-box fuzzing methodology for EVCS firmware using ISO 15118 state-machine paths.

## Industry Whitepapers and Practitioner Reports

- [Securing the Charge: Hidden Risks in ISO 15118](https://cdn.vicone.com/files/research-papers/EN/vicone_securing_the_charge_hidden_risks_in_iso15118.pdf)  
  Practitioner-oriented security analysis discussing why ISO 15118 compliance alone does not provide end-to-end charging security.

- [Is ISO 15118 Enough to Secure EV Charging?](https://vicone.com/blog/is-iso-15118-enough-to-secure-ev-charging)
  Short practitioner summary accompanying the VicOne ISO 15118 security paper.
  
