import com.siemens.ct.exi.core.EXIFactory;
import com.siemens.ct.exi.core.FidelityOptions;
import com.siemens.ct.exi.core.exceptions.EXIException;
import com.siemens.ct.exi.core.grammars.Grammars;
import com.siemens.ct.exi.core.helpers.DefaultEXIFactory;
import com.siemens.ct.exi.grammars.GrammarFactory;
import com.siemens.ct.exi.main.api.sax.EXIResult;
import com.siemens.ct.exi.main.api.sax.EXISource;

import org.xml.sax.InputSource;
import org.xml.sax.XMLReader;

import javax.xml.parsers.SAXParserFactory;
import javax.xml.transform.OutputKeys;
import javax.xml.transform.Transformer;
import javax.xml.transform.TransformerFactory;
import javax.xml.transform.sax.SAXSource;
import javax.xml.transform.stream.StreamResult;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;

/**
 * Independent second EXI oracle (Siemens EXIficient, a generic W3C-EXI-spec processor) used to
 * cross-validate the XMLDSig fragment wire encoding produced by our generator and already
 * byte-diffed against cbV2G. Encodes/decodes schema-informed EXI (bit-packed, non-strict fidelity
 * — the same convention cbV2G/cbexigen and our generator use) against an XSD entry point that
 * pulls in the rest of a message set's schema via its own {@code <xs:import>} chain.
 *
 * Development tool only — not part of `dotnet test`. See README.md.
 */
public class ExificientRef {

    public static void main(String[] args) throws Exception {
        if (args.length >= 3 && "primitives".equals(args[0])) {
            primitives(args[1], args[2]);
            return;
        }
        if (args.length < 5) {
            usage();
            System.exit(2);
        }

        String mode = args[0];
        switch (mode) {
            case "encode":
                encode(args[1], "fragment".equals(args[2]), args[3], args[4]);
                break;
            case "decode":
                decode(args[1], "fragment".equals(args[2]), args[3], args[4]);
                break;
            default:
                usage();
                System.exit(2);
        }
    }

    private static void usage() {
        System.err.println("usage: ExificientRef encode <xsd-entry-point> <fragment|document> <in.xml> <out.hex>");
        System.err.println("       ExificientRef decode <xsd-entry-point> <fragment|document> <in.hex> <out.xml>");
        System.err.println("       ExificientRef primitives <in.tsv> <out.tsv>   (schema-less EXI §7.1 datatypes)");
    }

    private static EXIFactory buildFactory(String xsdPath, boolean fragment) throws EXIException {
        Grammars grammars = GrammarFactory.newInstance().createGrammars(xsdPath);
        EXIFactory ef = DefaultEXIFactory.newInstance();
        ef.setGrammars(grammars);
        ef.setFidelityOptions(FidelityOptions.createDefault());
        ef.setFragment(fragment);
        if (System.getenv("EXIF_CANONICAL") != null) {
            try {
                ef.getEncodingOptions().setOption(com.siemens.ct.exi.core.EncodingOptions.CANONICAL_EXI);
            } catch (Exception e) { System.err.println("no canonical option: " + e); }
        }
        return ef;
    }

    /**
     * Schema-less EXI primitive datatypes (EXI 1.0 §7.1), encoded through EXIficient's own
     * {@link com.siemens.ct.exi.core.io.channel.BitEncoderChannel} — the layer beneath grammars and
     * value tables. This is the oracle for {@code Primitives.vectors.json}, whose expected bytes were
     * previously self-encoded by the codec under test.
     *
     * <p>Input/output are TSV rather than JSON so this tool needs no JSON dependency; the caller
     * (see {@code tools/exificient-ref/primitives.py}) converts to and from the vector file.
     * Input columns: {@code name <TAB> datatype <TAB> value}. Output: {@code name <TAB> hex}.
     *
     * <p><b>String note.</b> {@code encodeString} writes a bare length prefix. ISO 15118's
     * schema-less string *values* use the value-table miss framing — length + 2 — which lives one
     * layer above this channel, so it is expressed explicitly here as
     * {@code encodeUnsignedInteger(len + 2)} followed by {@code encodeStringOnly}. The character
     * encoding itself is still EXIficient's.
     */
    private static void primitives(String inTsv, String outTsv) throws Exception {
        StringBuilder out = new StringBuilder();

        for (String line : Files.readAllLines(Paths.get(inTsv), StandardCharsets.UTF_8)) {
            if (line.isEmpty()) continue;
            String[] parts = line.split("\t", 3);
            String name = parts[0], datatype = parts[1], value = parts.length > 2 ? parts[2] : "";

            ByteArrayOutputStream bos = new ByteArrayOutputStream();
            com.siemens.ct.exi.core.io.channel.BitEncoderChannel ch =
                    new com.siemens.ct.exi.core.io.channel.BitEncoderChannel(bos);

            switch (datatype) {
                case "unsignedInteger":
                    ch.encodeUnsignedInteger(Integer.parseInt(value));
                    break;
                case "signedInteger":
                    ch.encodeInteger(Integer.parseInt(value));
                    break;
                case "boolean":
                    ch.encodeBoolean(Boolean.parseBoolean(value));
                    break;
                case "binary": {
                    String hex = value.replaceAll("\\s+", "");
                    byte[] raw = new byte[hex.length() / 2];
                    for (int i = 0; i < raw.length; i++)
                        raw[i] = (byte) Integer.parseInt(hex.substring(i * 2, i * 2 + 2), 16);
                    ch.encodeBinary(raw);
                    break;
                }
                case "string":
                    ch.encodeUnsignedInteger(value.codePointCount(0, value.length()) + 2);
                    ch.encodeStringOnly(value);
                    break;
                default:
                    throw new IllegalArgumentException("unknown datatype: " + datatype);
            }

            ch.flush();
            byte[] bytes = bos.toByteArray();
            StringBuilder hex = new StringBuilder();
            for (int i = 0; i < bytes.length; i++) {
                if (i > 0) hex.append(' ');
                hex.append(String.format("%02x", bytes[i] & 0xFF));
            }
            out.append(name).append('\t').append(hex).append('\n');
        }

        Files.write(Paths.get(outTsv), out.toString().getBytes(StandardCharsets.UTF_8));
        System.out.print(out);
    }

    private static void encode(String xsdPath, boolean fragment, String inXml, String outHex) throws Exception {
        EXIFactory ef = buildFactory(xsdPath, fragment);

        SAXParserFactory spf = SAXParserFactory.newInstance();
        spf.setNamespaceAware(true);
        XMLReader xmlReader = spf.newSAXParser().getXMLReader();

        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        EXIResult exiResult = new EXIResult(ef);
        exiResult.setOutputStream(bos);

        try (FileInputStream in = new FileInputStream(inXml)) {
            SAXSource source = new SAXSource(xmlReader, new InputSource(in));
            TransformerFactory.newInstance().newTransformer().transform(source, exiResult);
        }

        byte[] bytes = bos.toByteArray();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < bytes.length; i++) {
            if (i > 0) sb.append(' ');
            sb.append(String.format("%02x", bytes[i] & 0xFF));
        }
        Files.write(Paths.get(outHex), sb.toString().getBytes(StandardCharsets.US_ASCII));
        System.out.println(sb);
    }

    private static void decode(String xsdPath, boolean fragment, String inHex, String outXml) throws Exception {
        EXIFactory ef = buildFactory(xsdPath, fragment);

        String hex = new String(Files.readAllBytes(Paths.get(inHex)), StandardCharsets.US_ASCII)
                .replaceAll("\\s+", "");
        byte[] bytes = new byte[hex.length() / 2];
        for (int i = 0; i < bytes.length; i++) {
            bytes[i] = (byte) Integer.parseInt(hex.substring(i * 2, i * 2 + 2), 16);
        }

        EXISource exiSource = new EXISource(ef);
        SAXSource saxSource = new SAXSource(exiSource.getXMLReader(), new InputSource(new ByteArrayInputStream(bytes)));

        Transformer transformer = TransformerFactory.newInstance().newTransformer();
        transformer.setOutputProperty(OutputKeys.OMIT_XML_DECLARATION, "yes");
        transformer.setOutputProperty(OutputKeys.INDENT, "yes");
        try (OutputStream os = new FileOutputStream(outXml)) {
            transformer.transform(saxSource, new StreamResult(os));
        }
        System.out.println(new String(Files.readAllBytes(Paths.get(outXml)), StandardCharsets.UTF_8));
    }
}
