export default {
  async fetch(request, env, ctx) {
    return new Response(JSON.stringify({
      status: "ok",
      worker: "alpha-contact-spam-filter",
      version: "1.0.0",
      timestamp: new Date().toISOString()
    }), {
      status: 200,
      headers: {
        "Content-Type": "application/json"
      }
    });
  },
  async email(message, env, ctx) {
    
    const from = message.from;
    const to = message.to;
    const subject = message.headers.get("subject") || "(no subject)";
    
    // Check if this is from your contact form
    const isFromContactForm = message.headers.get("X-Alpha-Contact-Form") === "true";
    const isTurnstileVerified = message.headers.get("X-Turnstile-Verified") === "true";
    const sentFrom = message.headers.get("X-Sent-From");
    
    const spamScore = message.spamScore || 0;
    
    console.log("==========================================");
    console.log("[Email Worker] New email received");
    console.log(`[Email Worker] From: ${from}`);
    console.log(`[Email Worker] To: ${to}`);
    console.log(`[Email Worker] Subject: ${subject}`);
    console.log(`[Email Worker] Spam Score: ${spamScore}/10`);
    console.log(`[Email Worker] From Contact Form: ${isFromContactForm}`);
    console.log(`[Email Worker] Turnstile Verified: ${isTurnstileVerified}`);
    console.log(`[Email Worker] Sent From: ${sentFrom}`);
    console.log("==========================================");
    
    try {
      // **IMPORTANT: Verified contact form emails ALWAYS go through**
      if (isFromContactForm && isTurnstileVerified && sentFrom === "Alpha-Contact-Application") {
        console.log("[Email Worker] ✅ VERIFIED contact form submission");
        console.log("[Email Worker] ⚠️ BYPASSING ALL SPAM CHECKS");
        console.log("[Email Worker] Action: Forward to info@alphaayakkabi.com");
        
        await message.forward("info@alphaayakkabi.com");
        console.log("[Email Worker] ✅ Successfully forwarded");
        return;
      }
      
      // For external emails, validate From header
      if (!from || from.trim() === "" || from === "-") {
        console.log("[Email Worker] ❌ INVALID FROM ADDRESS");
        console.log("[Email Worker] Action: REJECT");
        
        message.setReject("Invalid or missing From address");
        return;
      }
      
      console.log("[Email Worker] ⚠️ External email detected (not from contact form)");
      console.log("[Email Worker] Applying spam filtering...");
      
      // High spam score = reject
      if (spamScore >= 7) {
        console.log(`[Email Worker] ❌ HIGH SPAM SCORE: ${spamScore}/10`);
        console.log("[Email Worker] Action: REJECT");
        
        message.setReject(`Spam detected with score ${spamScore}/10`);
        return;
      }
      
      // Medium spam score = quarantine
      if (spamScore >= 4) {
        console.log(`[Email Worker] ⚠️ MEDIUM SPAM SCORE: ${spamScore}/10`);
        console.log("[Email Worker] Action: Send to quarantine");
        
        await message.forward("quarantine@alphaayakkabi.com");
        console.log("[Email Worker] ✅ Sent to quarantine");
        return;
      }
      
      // Low spam score = forward normally
      console.log(`[Email Worker] ✅ LOW SPAM SCORE: ${spamScore}/10`);
      console.log("[Email Worker] Action: Forward to info@alphaayakkabi.com");
      
      await message.forward("info@alphaayakkabi.com");
      console.log("[Email Worker] ✅ Successfully forwarded");
      
    } catch (error) {
      console.error("[Email Worker] ❌ ERROR:", error.message);
      console.error("[Email Worker] Stack:", error.stack);
      // Don't throw - let the email be delivered anyway
      await message.forward("info@alphaayakkabi.com");
    }
  }
}